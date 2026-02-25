using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBH.Data;
using WebBH.Models;

namespace WebBH.Controllers
{
    // BỎ [Authorize] ở đây để ai cũng bấm nút được, ta sẽ tự check bên dưới
    public class CartController : Controller
    {
        private readonly WebThanhLyDbContext _context;
        public CartController(WebThanhLyDbContext context) => _context = context;

        // 1. THÊM VÀO GIỎ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity, string? size, string? color)
        {
            // NẾU CHƯA ĐĂNG NHẬP -> Trả về JSON báo hiệu cần đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { requireLogin = true, redirectUrl = Url.Action("Login", "Account") });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            string errorMessage = await ProcessAddToCart(userId, productId, quantity, size, color);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                return Json(new { success = false, message = errorMessage });
            }

            var product = await _context.Products.FindAsync(productId);
            var totalCount = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);

            return Json(new
            {
                success = true,
                count = totalCount,
                newItem = new { name = product.Name, imageUrl = product.ImageUrl, price = product.Price.ToString("N0") + " đ", size, color }
            });
        }

        // 2. MUA NGAY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int productId, int quantity, string? size, string? color)
        {
            // NẾU CHƯA ĐĂNG NHẬP -> Trả về JSON báo hiệu cần đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { requireLogin = true, redirectUrl = Url.Action("Login", "Account") });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            string errorMessage = await ProcessAddToCart(userId, productId, quantity, size, color);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                return Json(new { success = false, message = errorMessage });
            }

            return Json(new { success = true, redirectUrl = Url.Action("Checkout", "Cart") });
        }

        // --- HÀM XỬ LÝ CHÍNH ---
        private async Task<string> ProcessAddToCart(int userId, int productId, int quantity, string? size, string? color)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return "Sản phẩm không tồn tại.";

            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v =>
                v.ProductId == productId &&
                (string.IsNullOrEmpty(size) ? (v.Size == null || v.Size == "") : v.Size == size) &&
                (string.IsNullOrEmpty(color) ? (v.Color == null || v.Color == "") : v.Color == color)
            );

            if (variant == null) return "Phiên bản sản phẩm không hợp lệ.";

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.ProductId == productId &&
                (c.Size ?? "") == (size ?? "") &&
                (c.Color ?? "") == (color ?? "")
            );

            int currentQtyInCart = cartItem?.Quantity ?? 0;
            int totalRequested = currentQtyInCart + quantity;

            if (totalRequested > variant.Quantity)
            {
                return $"Kho chỉ còn {variant.Quantity} sản phẩm. Bạn đã có {currentQtyInCart} trong giỏ hàng.";
            }

            if (cartItem != null)
            {
                cartItem.Quantity = totalRequested;
                _context.Update(cartItem);
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    Size = size,
                    Color = color
                });
            }

            await _context.SaveChangesAsync();
            return "";
        }

        // 3. CÁC HÀM CÒN LẠI (Gắn lại [Authorize] để bảo mật)
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            var user = await _context.Users.FindAsync(userId);
            ViewBag.UserInfo = user;

            return View(cartItems);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int change)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var cartItem = await _context.CartItems.Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);

            if (cartItem == null) return NotFound();

            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.ProductId == cartItem.ProductId
                                       && (v.Size ?? "") == (cartItem.Size ?? "")
                                       && (v.Color ?? "") == (cartItem.Color ?? ""));

            int newQuantity = cartItem.Quantity + change;

            if (newQuantity < 1)
            {
                return Json(new { success = false, isMinError = true, message = "Số lượng tối thiểu là 1." });
            }

            if (variant != null && newQuantity > variant.Quantity)
            {
                return Json(new { success = false, isStockError = true, maxStock = variant.Quantity, message = $"Chỉ còn {variant.Quantity} sản phẩm." });
            }

            cartItem.Quantity = newQuantity;
            _context.Update(cartItem);
            await _context.SaveChangesAsync();

            var cartTotal = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity * c.Product.Price);

            return Json(new
            {
                success = true,
                newQuantity,
                itemTotal = (cartItem.Quantity * cartItem.Product.Price).ToString("N0") + " đ",
                cartTotal = cartTotal.ToString("N0") + " đ"
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);
            if (cartItem != null) { _context.CartItems.Remove(cartItem); await _context.SaveChangesAsync(); }

            var cartTotal = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity * c.Product.Price);
            var count = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);

            return Json(new { success = true, cartTotal = cartTotal.ToString("N0") + " đ", count });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(string fullName, string phone, string address)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Product");

            foreach (var item in cartItems)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.ProductId == item.ProductId
                                           && (v.Size ?? "") == (item.Size ?? "")
                                           && (v.Color ?? "") == (item.Color ?? ""));

                if (variant == null || variant.Quantity < item.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm {item.Product.Name} đã hết hàng hoặc không đủ số lượng!";
                    return RedirectToAction("Checkout");
                }
                variant.Quantity -= item.Quantity;
                _context.Update(variant);
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = cartItems.Sum(x => x.Quantity * x.Product.Price),
                Status = "Pending",
                ReceiverName = fullName,
                ReceiverPhone = phone,
                ShippingAddress = address
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price,
                    Size = item.Size,
                    Color = item.Color
                });
            }
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderSuccess", new { id = order.OrderId });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return RedirectToAction("Index", "Home");
            return View(order);
        }
    }
}
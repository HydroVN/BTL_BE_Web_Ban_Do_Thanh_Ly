using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBH.Data;
using WebBH.Models;

namespace WebBH.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly WebThanhLyDbContext _context;
        public CartController(WebThanhLyDbContext context) => _context = context;
        // 1. THÊM VÀO GIỎ & MUA NGAY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity, string size, string color)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();
            bool result = await ProcessAddToCart(userId, productId, quantity, size, color);
            if (!result) return NotFound();
            var product = await _context.Products.FindAsync(productId);
            var totalCount = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new
            {
                success = true,
                count = totalCount,
                newItem = new { name = product.Name, imageUrl = product.ImageUrl, price = product.Price.ToString("N0") + " đ", size, color }
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int productId, int quantity, string size, string color)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            await ProcessAddToCart(userId, productId, quantity, size, color);
            return RedirectToAction("Checkout");
        }
        private async Task<bool> ProcessAddToCart(int userId, int productId, int quantity, string size, string color)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(c =>
                c.UserId == userId && c.ProductId == productId && c.Size == size && c.Color == color);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
                _context.Update(cartItem);
            }
            else
            {
                _context.CartItems.Add(new CartItem { UserId = userId, ProductId = productId, Quantity = quantity, Size = size, Color = color });
            }
            await _context.SaveChangesAsync();
            return true;
        }
        // 2. QUẢN LÝ GIỎ HÀNG (CHECKOUT)
        public async Task<IActionResult> Checkout()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) 
                return RedirectToAction("Login", "Account");
            // Lấy giỏ hàng
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();
            // LẤY THÔNG TIN USER ĐỂ ĐIỀN SẴN VÀO FORM
            var user = await _context.Users.FindAsync(userId);
            ViewBag.UserInfo = user; // Truyền user qua ViewBag

            return View(cartItems);
        }
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
                                       && v.Size == cartItem.Size
                                       && v.Color == cartItem.Color);

            int newQuantity = cartItem.Quantity + change;

            // === 1. CHECK SỐ LƯỢNG TỐI THIỂU ===
            if (newQuantity < 1)
            {
                return Json(new
                {
                    success = false,
                    isMinError = true, // Cờ báo lỗi tối thiểu
                    message = "Số lượng tối thiểu là 1 sản phẩm."
                });
            }

            // === 2. CHECK TỒN KHO ===
            if (variant != null && newQuantity > variant.Quantity)
            {
                return Json(new
                {
                    success = false,
                    isStockError = true, // Cờ báo lỗi tồn kho
                    maxStock = variant.Quantity,
                    message = $"Chỉ còn {variant.Quantity} sản phẩm trong kho."
                });
            }

            // Cập nhật hợp lệ
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
        // 3. XÁC NHẬN ĐẶT HÀNG & THÀNH CÔNG
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
            // 1. KIỂM TRA VÀ TRỪ TỒN KHO
            foreach (var item in cartItems)
            {
                // Tìm đúng biến thể (Size/Màu) trong kho
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.ProductId == item.ProductId
                                           && v.Size == item.Size
                                           && v.Color == item.Color);

                // Nếu kho không có hoặc không đủ hàng
                if (variant == null || variant.Quantity < item.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm {item.Product.Name} ({item.Size}/{item.Color}) đã hết hàng hoặc không đủ số lượng!";
                    return RedirectToAction("Checkout");
                }
                // Trừ kho
                variant.Quantity -= item.Quantity;
                _context.Update(variant);
            }
            // 2. TẠO ĐƠN HÀNG
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
            await _context.SaveChangesAsync(); // Lưu đơn hàng và cập nhật kho
            // 3. TẠO CHI TIẾT ĐƠN (LƯU SIZE/COLOR)
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
        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return RedirectToAction("Index", "Home");
            return View(order);
        }
    }
}
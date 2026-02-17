using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBH.Data;
using WebBH.Models;

namespace WebBH.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly WebThanhLyDbContext _context;

        public OrderController(WebThanhLyDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH ĐƠN HÀNG (LỊCH SỬ)
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // 2. CHI TIẾT ĐƠN HÀNG
        public async Task<IActionResult> Details(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null) return NotFound();

            return View(order);
        }

        // 3. XỬ LÝ: HỦY ĐƠN HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId, string reason)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");
            var order = await _context.Orders
                .Include(o => o.OrderDetails) // Lấy chi tiết đơn
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
            if (order == null) return NotFound();
            if (order.Status == "Pending" || order.Status == "Packing")
            {
                // === LOGIC HOÀN KHO CHÍNH XÁC ===
                foreach (var detail in order.OrderDetails)
                {
                    // Tìm đúng biến thể dựa trên Size và Color đã lưu
                    var variant = await _context.ProductVariants
                        .FirstOrDefaultAsync(v => v.ProductId == detail.ProductId
                                               && v.Size == detail.Size
                                               && v.Color == detail.Color);

                    if (variant != null)
                    {
                        variant.Quantity += detail.Quantity; // Cộng lại số lượng
                        _context.Update(variant);
                    }
                }
                // =================================
                order.Status = "Cancelled";
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã hủy đơn hàng và hoàn tiền vào kho thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đơn hàng này.";
            }
            return RedirectToAction("Index");
        }

        // 4. XỬ LÝ: ĐÃ NHẬN ĐƯỢC HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int orderId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null) return NotFound();

            // Logic: Chỉ cho xác nhận khi đang giao hàng (Shipping)
            if (order.Status == "Shipping")
            {
                order.Status = "Success"; // Chuyển thành Hoàn thành (Success hoặc Completed tùy bạn)
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Cảm ơn bạn đã mua hàng!";
            }
            else
            {
                TempData["Error"] = "Trạng thái đơn hàng không hợp lệ để xác nhận.";
            }

            return RedirectToAction("Index"); // Quay về danh sách thay vì chi tiết
        }
    }
}
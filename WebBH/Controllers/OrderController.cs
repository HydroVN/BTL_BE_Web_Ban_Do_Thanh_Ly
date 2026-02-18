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

        // 1. DANH SÁCH ĐƠN HÀNG (Có lọc theo status)
        public async Task<IActionResult> Index(string status)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            // Tạo câu truy vấn cơ bản
            var query = _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId);

            // Xử lý lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "pending": // Tab: Chờ thanh toán
                        query = query.Where(o => o.Status == "Pending" || o.Status == "Packing");
                        break;
                    case "shipping": // Tab: Đang giao
                        query = query.Where(o => o.Status == "Shipping");
                        break;
                    case "completed": // Tab: Hoàn thành
                        query = query.Where(o => o.Status == "Success" || o.Status == "Completed");
                        break;
                    case "cancelled": // Tab: Đã hủy
                        query = query.Where(o => o.Status == "Cancelled");
                        break;
                }
            }

            // Sắp xếp giảm dần theo ngày
            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            // Lấy danh sách đánh giá để hiển thị nút "Đánh giá" hoặc "Xem đánh giá"
            var reviews = await _context.Reviews.Where(r => r.UserId == userId).ToListAsync();
            ViewBag.UserReviews = reviews;

            // Truyền trạng thái hiện tại sang View để tô màu nút đang chọn
            ViewBag.CurrentStatus = status;

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

        // 3. HỦY ĐƠN HÀNG (Có hoàn kho)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId, string reason)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null) return NotFound();

            if (order.Status == "Pending" || order.Status == "Packing")
            {
                // Hoàn lại số lượng tồn kho
                foreach (var detail in order.OrderDetails)
                {
                    var variant = await _context.ProductVariants
                        .FirstOrDefaultAsync(v => v.ProductId == detail.ProductId && v.Size == detail.Size && v.Color == detail.Color);

                    if (variant != null)
                    {
                        variant.Quantity += detail.Quantity;
                        _context.Update(variant);
                    }
                }

                order.Status = "Cancelled";
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã hủy đơn hàng thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đơn hàng này.";
            }
            return RedirectToAction("Index");
        }

        // 4. XÁC NHẬN ĐÃ NHẬN HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int orderId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order != null && order.Status == "Shipping")
            {
                order.Status = "Completed"; // Hoặc "Success" tùy database của bạn
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã xác nhận nhận hàng thành công!";
            }
            else
            {
                TempData["Error"] = "Trạng thái đơn hàng không hợp lệ.";
            }

            return RedirectToAction("Index");
        }
    }
}
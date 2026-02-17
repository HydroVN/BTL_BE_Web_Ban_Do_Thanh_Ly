using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBH.Data;

namespace WebBH.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly WebThanhLyDbContext _context;
        public OrdersController(WebThanhLyDbContext context) => _context = context;
        // 1. DANH SÁCH ĐƠN HÀNG (CÓ TÌM KIẾM)
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Orders
                .Include(u => u.User)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();

                // Tìm theo ID (nếu là số)
                if (int.TryParse(searchString, out int id))
                {
                    query = query.Where(o => o.OrderId == id);
                }
                // Tìm theo Tên hoặc Email
                else
                {
                    query = query.Where(o =>
                        (o.User.FullName != null && o.User.FullName.Contains(searchString)) ||
                        (o.User.Email != null && o.User.Email.Contains(searchString)));
                }
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await query.ToListAsync());
        }
        // 2. CHI TIẾT ĐƠN HÀNG
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
        }
        // 3. CẬP NHẬT TRẠNG THÁI (CÓ BẢO MẬT)
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order != null)
            {
                // Chặn sửa nếu đơn đã chốt
                if (order.Status == "Cancelled" || order.Status == "Success" || order.Status == "Completed")
                {
                    return RedirectToAction("Index");
                }
                // === NẾU ADMIN HỦY ĐƠN -> HOÀN KHO ===
                if (status == "Cancelled")
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var variant = await _context.ProductVariants
                            .FirstOrDefaultAsync(v => v.ProductId == detail.ProductId
                                                   && v.Size == detail.Size
                                                   && v.Color == detail.Color);
                        if (variant != null)
                        {
                            variant.Quantity += detail.Quantity; // Cộng kho
                            _context.Update(variant);
                        }
                    }
                }
                order.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
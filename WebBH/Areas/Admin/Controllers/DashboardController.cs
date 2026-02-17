using Microsoft.AspNetCore.Mvc;
using WebBH.Data;
using WebBH.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace WebBH.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly WebThanhLyDbContext _context;

        public DashboardController(WebThanhLyDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var currentYear = DateTime.Now.Year;

            // 1. TÍNH DOANH THU THEO THÁNG (CHỈ TÍNH ĐƠN THÀNH CÔNG)
            var successOrders = _context.Orders
                .Where(o => o.OrderDate.HasValue
                            && o.OrderDate.Value.Year == currentYear
                            && (o.Status == "Success" || o.Status == "Completed")) // Logic quan trọng
                .Select(o => new { Month = o.OrderDate.Value.Month, TotalAmount = o.TotalAmount })
                .ToList();

            var monthlyRevenue = new List<decimal>();
            for (int i = 1; i <= 12; i++)
            {
                var rev = successOrders.Where(o => o.Month == i).Sum(o => o.TotalAmount) ?? 0;
                monthlyRevenue.Add(rev);
            }

            // 2. TOP SẢN PHẨM BÁN CHẠY (Dựa trên đơn thành công)
            var topSelling = _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Order.Status == "Success" || od.Order.Status == "Completed") // Logic quan trọng
                .AsEnumerable()
                .GroupBy(od => od.Product)
                .Select(g => new TopProductVM
                {
                    ProductName = g.Key.Name,
                    Price = g.Key.Price,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();

            // 3. TỔNG HỢP DATA
            var vm = new DashboardVM
            {
                TotalUsers = _context.Users.Count(),
                TotalProducts = _context.Products.Count(),
                TotalOrders = _context.Orders.Count(),
                // Tổng doanh thu toàn thời gian (Chỉ tính đơn thành công)
                TotalRevenue = _context.Orders
                    .Where(o => o.Status == "Success" || o.Status == "Completed")
                    .Sum(x => x.TotalAmount) ?? 0,
                RevenueByMonth = monthlyRevenue,
                TopProducts = topSelling
            };

            return View(vm);
        }
    }
}
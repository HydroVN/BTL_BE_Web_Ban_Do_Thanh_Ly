using Microsoft.AspNetCore.Mvc;
using WebBH.Data;
using WebBH.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using ClosedXML.Excel; // Thêm thư viện xuất Excel
using System.IO;       // Thêm thư viện xử lý File

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
                            && (o.Status == "Success" || o.Status == "Completed"))
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
                .Where(od => od.Order.Status == "Success" || od.Order.Status == "Completed")
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
                TotalRevenue = _context.Orders
                    .Where(o => o.Status == "Success" || o.Status == "Completed")
                    .Sum(x => x.TotalAmount) ?? 0,
                RevenueByMonth = monthlyRevenue,
                TopProducts = topSelling
            };

            return View(vm);
        }

        // ==========================================================
        // 4. API XUẤT BÁO CÁO EXCEL TỪ DASHBOARD
        // ==========================================================
        public IActionResult ExportExcel()
        {
            var currentYear = DateTime.Now.Year;

            // Lấy lại data giống hàm Index
            var successOrders = _context.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Year == currentYear && (o.Status == "Success" || o.Status == "Completed"))
                .Select(o => new { Month = o.OrderDate.Value.Month, TotalAmount = o.TotalAmount })
                .ToList();

            var monthlyRevenue = new List<decimal>();
            for (int i = 1; i <= 12; i++)
            {
                monthlyRevenue.Add(successOrders.Where(o => o.Month == i).Sum(o => o.TotalAmount) ?? 0);
            }

            var topSelling = _context.OrderDetails.Include(od => od.Product)
                .Where(od => od.Order.Status == "Success" || od.Order.Status == "Completed")
                .AsEnumerable()
                .GroupBy(od => od.Product)
                .Select(g => new TopProductVM
                {
                    ProductName = g.Key.Name,
                    Price = g.Key.Price,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.UnitPrice)
                }).OrderByDescending(x => x.TotalSold).Take(5).ToList();

            var totalUsers = _context.Users.Count();
            var totalProducts = _context.Products.Count();
            var totalOrders = _context.Orders.Count();
            var totalRevenue = _context.Orders.Where(o => o.Status == "Success" || o.Status == "Completed").Sum(x => x.TotalAmount) ?? 0;

            // Bắt đầu tạo file Excel
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("BaoCaoTongQuan");
                var currentRow = 1;

                // Tiêu đề
                worksheet.Cell(currentRow, 1).Value = $"BÁO CÁO TỔNG QUAN NĂM {currentYear}";
                worksheet.Range("A1:D1").Merge().Style.Font.SetBold().Font.FontSize = 16;
                worksheet.Range("A1:D1").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Range("A2:D2").Merge().Style.Font.SetItalic();
                currentRow += 2;

                // --- PHẦN 1: THỐNG KÊ CHUNG ---
                worksheet.Cell(currentRow, 1).Value = "TỔNG QUAN HỆ THỐNG";
                worksheet.Range(currentRow, 1, currentRow, 2).Merge().Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 2).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Tổng khách hàng:";
                worksheet.Cell(currentRow, 2).Value = totalUsers;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Tổng sản phẩm:";
                worksheet.Cell(currentRow, 2).Value = totalProducts;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Tổng đơn hàng:";
                worksheet.Cell(currentRow, 2).Value = totalOrders;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Tổng doanh thu (VNĐ):";
                worksheet.Cell(currentRow, 2).Value = totalRevenue;
                worksheet.Cell(currentRow, 2).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 2).Style.Font.FontColor = XLColor.Red;
                currentRow += 2;

                // --- PHẦN 2: DOANH THU THEO THÁNG ---
                worksheet.Cell(currentRow, 1).Value = "DOANH THU THEO THÁNG";
                worksheet.Range(currentRow, 1, currentRow, 2).Merge().Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 2).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Tháng";
                worksheet.Cell(currentRow, 2).Value = "Doanh Thu (VNĐ)";
                worksheet.Range(currentRow, 1, currentRow, 2).Style.Font.Bold = true;

                int monthStartRow = currentRow;
                for (int i = 0; i < 12; i++)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = $"Tháng {i + 1}";
                    worksheet.Cell(currentRow, 2).Value = monthlyRevenue[i];
                    worksheet.Cell(currentRow, 2).Style.NumberFormat.Format = "#,##0";
                }
                worksheet.Range(monthStartRow, 1, currentRow, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(monthStartRow, 1, currentRow, 2).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                currentRow += 2;

                // --- PHẦN 3: TOP SẢN PHẨM ---
                worksheet.Cell(currentRow, 1).Value = "TOP 5 SẢN PHẨM BÁN CHẠY NHẤT";
                worksheet.Range(currentRow, 1, currentRow, 4).Merge().Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Tên Sản Phẩm";
                worksheet.Cell(currentRow, 2).Value = "Đơn Giá (VNĐ)";
                worksheet.Cell(currentRow, 3).Value = "Đã Bán";
                worksheet.Cell(currentRow, 4).Value = "Doanh Thu (VNĐ)";
                worksheet.Range(currentRow, 1, currentRow, 4).Style.Font.Bold = true;

                int topProdStartRow = currentRow;
                foreach (var item in topSelling)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.ProductName;
                    worksheet.Cell(currentRow, 2).Value = item.Price;
                    worksheet.Cell(currentRow, 2).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(currentRow, 3).Value = item.TotalSold;
                    worksheet.Cell(currentRow, 4).Value = item.TotalRevenue;
                    worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0";
                }
                if (topSelling.Any())
                {
                    worksheet.Range(topProdStartRow, 1, currentRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(topProdStartRow, 1, currentRow, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                }

                // Căn chỉnh tự động kích thước cột
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"BaoCaoTongQuan_{DateTime.Now:ddMMyyyy_HHmm}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBH.Data;
using WebBH.Models;
namespace WebBH.Controllers
{
    public class ReviewController : Controller
    {
        private readonly WebThanhLyDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReviewController(WebThanhLyDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        [HttpPost]
        public async Task<IActionResult> SubmitReview(int productId, int orderId, int rating, string comment, List<IFormFile> mediaFiles)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Json(new { success = false, message = "Vui lòng đăng nhập!" });
            // 1. KIỂM TRA ĐIỀU KIỆN
            var order = await _context.Orders.Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
            if (order == null) return Json(new { success = false, message = "Đơn hàng không tồn tại." });
            if (order.Status != "Success" && order.Status != "Completed")
            {
                return Json(new { success = false, message = "Bạn phải nhận hàng thành công mới được đánh giá!" });
            }
            // Check sản phẩm có trong đơn ko
            if (!order.OrderDetails.Any(od => od.ProductId == productId))
                return Json(new { success = false, message = "Sản phẩm này không có trong đơn hàng." });
            // Check đã đánh giá chưa
            var exists = await _context.Reviews.AnyAsync(r => r.OrderId == orderId && r.ProductId == productId);
            if (exists)
            {
                //Lưu thông báo lỗi và trả về tín hiệu để Client reload trang
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này rồi! Vui lòng kiểm tra lại.";
                return Json(new { success = false, exist = true });
            }
            // 2. UPLOAD ẢNH/VIDEO
            string mediaPaths = "";
            if (mediaFiles != null && mediaFiles.Count > 0)
            {
                var uploadedPaths = new List<string>();
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads/reviews");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                foreach (var file in mediaFiles)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string savePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    uploadedPaths.Add("/uploads/reviews/" + fileName);
                }
                mediaPaths = string.Join(";", uploadedPaths);
            }
            // 3. LƯU DATABASE
            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                OrderId = orderId,
                Rating = rating,
                Comment = comment,
                MediaUrl = mediaPaths,
                CreatedAt = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            //Thêm thông báo vào TempData 
            TempData["Message"] = "Cảm ơn bạn! Đánh giá sản phẩm thành công.";
            return Json(new { success = true, message = "Đánh giá thành công!" });
        }
    }
}
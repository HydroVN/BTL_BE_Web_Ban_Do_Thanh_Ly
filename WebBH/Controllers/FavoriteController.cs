using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBH.Data;
using WebBH.Models;

namespace WebBH.Controllers
{
    [Authorize]
    public class FavoriteController : Controller
    {
        private readonly WebThanhLyDbContext _context;

        public FavoriteController(WebThanhLyDbContext context)
        {
            _context = context;
        }

        // 1. Xem danh sách yêu thích
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var favorites = await _context.FavoriteProducts
                .Include(f => f.Product) // Lấy thông tin sản phẩm
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(favorites);
        }

        // 2. API Thêm/Xóa yêu thích (Dùng AJAX gọi)
        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var existing = await _context.FavoriteProducts
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (existing == null)
            {
                // Chưa có -> Thêm vào
                var fav = new FavoriteProduct
                {
                    UserId = userId,
                    ProductId = productId
                };
                _context.FavoriteProducts.Add(fav);
                await _context.SaveChangesAsync();
                return Json(new { success = true, status = "added", message = "Đã thêm vào yêu thích!" });
            }
            else
            {
                // Đã có -> Xóa đi
                _context.FavoriteProducts.Remove(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, status = "removed", message = "Đã bỏ yêu thích!" });
            }
        }
    }
}
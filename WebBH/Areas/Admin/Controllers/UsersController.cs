using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBH.Data;
using WebBH.Models;

namespace WebBH.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Users/[action]")] // Ép đường dẫn là Admin/Users cho đúng ý bạn
    public class UserController : Controller
    {
        private readonly WebThanhLyDbContext _context;

        public UserController(WebThanhLyDbContext context) { _context = context; }

        [Route("/Admin/Users")] // Đường dẫn mặc định cho trang danh sách
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Where(u => u.RoleId != 1) // Chỉ hiện User (RoleId 2), ẩn Admin (RoleId 1)
                .OrderByDescending(u => u.UserId)
                .ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int userId, string fullName, string email, string phone)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FullName = fullName;
            user.Email = email;
            user.PhoneNumber = phone;

            _context.Update(user);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Cập nhật thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int userId)
        {
            var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == userId);
            if (hasOrders)
            {
                TempData["Error"] = "User đã có đơn hàng, không thể xóa!";
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã xóa user!";
            }
            return RedirectToAction("Index");
        }
    }
}
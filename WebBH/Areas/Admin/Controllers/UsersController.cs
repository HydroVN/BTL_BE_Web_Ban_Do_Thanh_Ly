using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBH.Data;
using WebBH.Models;

namespace WebBH.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Users/[action]")]
    public class UserController : Controller
    {
        private readonly WebThanhLyDbContext _context;

        public UserController(WebThanhLyDbContext context) { _context = context; }

        [Route("/Admin/Users")]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Where(u => u.RoleId != 1)
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
        // HÀM KHÓA TÀI KHOẢN (BAN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendAccount(int userId, string reason, int durationDays, string notes)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.IsBanned = true;
            // Nối lý do và ghi chú để lưu vào DB
            user.BanReason = string.IsNullOrEmpty(notes) ? reason : $"{reason}. Ghi chú: {notes}";

            // Tính thời gian hết hạn
            if (durationDays == -1)
            {
                user.BannedUntil = null; // Bị khóa vĩnh viễn
            }
            else
            {
                user.BannedUntil = DateTime.Now.AddDays(durationDays);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Đã khóa tài khoản {user.Email} thành công.";

            return RedirectToAction("Index");
        }
        // HÀM MỞ KHÓA THỦ CÔNG (UNBAN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanAccount(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.IsBanned = false;
            user.BanReason = null;
            user.BannedUntil = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Đã mở khóa tài khoản {user.Email}.";

            return RedirectToAction("Index");
        }
    }
}
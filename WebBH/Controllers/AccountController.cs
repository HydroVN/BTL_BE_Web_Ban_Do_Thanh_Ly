using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBH.Data;
using WebBH.Models;
using WebBH.Services;

namespace WebBH.Controllers
{
    public class AccountController : Controller
    {
        private readonly WebThanhLyDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(WebThanhLyDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            var user = await _context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại";
                return View();
            }

            if (user.IsEmailConfirmed == false)
            {
                ViewBag.Error = "Tài khoản chưa được xác thực email. Vui lòng kiểm tra hộp thư!";
                return View();
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Sai mật khẩu";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            // SỬA TẠI ĐÂY: Dùng Equals để tránh lỗi phân biệt hoa thường
            if (string.Equals(user.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet] public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(Models.Register model)
        {
            if (!ModelState.IsValid) return View(model);
            var exists = await _context.Users.AnyAsync(x => x.Email == model.Email);
            if (exists) { ModelState.AddModelError("", "Email đã tồn tại"); return View(model); }
            var roleUser = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            string verificationCode = new Random().Next(100000, 999999).ToString();
            var user = new User { Email = model.Email, FullName = model.FullName, PhoneNumber = model.PhoneNumber, RoleId = roleUser.RoleId, IsEmailConfirmed = false, CreatedAt = DateTime.Now };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, model.Password);
            try
            {
                await _emailService.SendEmailAsync(model.Email, "Mã xác nhận RE:COLLECT", $"Mã xác nhận của bạn là: {verificationCode}");
                HttpContext.Session.SetString("VerificationCode", verificationCode);
                HttpContext.Session.SetString("PendingEmail", model.Email);
            }
            catch (Exception ex) { ModelState.AddModelError("", "Lỗi gửi email: " + ex.Message); return View(model); }
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("VerifyEmail");
        }

        [HttpGet] public IActionResult VerifyEmail() => View();

        [HttpPost]
        public async Task<IActionResult> VerifyEmail(string inputCode)
        {
            var sessionCode = HttpContext.Session.GetString("VerificationCode");
            var pendingEmail = HttpContext.Session.GetString("PendingEmail");
            if (inputCode == sessionCode)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == pendingEmail);
                if (user != null) { user.IsEmailConfirmed = true; await _context.SaveChangesAsync(); }
                return RedirectToAction("Login");
            }
            ViewBag.Error = "Mã xác nhận không chính xác!";
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            return View(new UserProfile { FullName = user.FullName, Email = user.Email, PhoneNumber = user.PhoneNumber });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UserProfile model)
        {
            if (!ModelState.IsValid) return View("Profile", model);
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == userEmail);
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name),
    
                // THÊM DÒNG NÀY: Phải giữ lại ID khi cập nhật hồ sơ, nếu không sẽ bị lỗi giỏ hàng
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return RedirectToAction("Profile");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
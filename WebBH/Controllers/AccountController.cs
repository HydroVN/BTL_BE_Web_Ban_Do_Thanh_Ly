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
using Microsoft.Extensions.Caching.Distributed; 

namespace WebBH.Controllers
{
    public class AccountController : Controller
    {
        private readonly WebThanhLyDbContext _context;
        private readonly EmailService _emailService;
        private readonly IDistributedCache _cache; 

        // CẬP NHẬT CONSTRUCTOR
        public AccountController(WebThanhLyDbContext context, EmailService emailService, IDistributedCache cache)
        {
            _context = context;
            _emailService = emailService;
            _cache = cache;
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
            // KIỂM TRA TÀI KHOẢN BỊ KHÓA & TỰ ĐỘNG UNBAN
            if (user.IsBanned)
            {
                if (user.BannedUntil.HasValue && user.BannedUntil.Value <= DateTime.Now)
                {
                    // Đã hết hạn -> Gỡ phạt
                    user.IsBanned = false;
                    user.BanReason = null;
                    user.BannedUntil = null;

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // 1. Tự động lấy độ lệch múi giờ của Server
                    TimeSpan serverOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
                    string offsetString = "UTC" + (serverOffset.TotalHours >= 0 ? "+" : "") + serverOffset.Hours;
                    // 2. Đang bị phạt -> Chuyển hướng báo lỗi KÈM THỜI GIAN và MÚI GIỜ
                    return RedirectToAction("AccountRestricted", "Account", new
                    {
                        reason = user.BanReason,
                        // Hiển thị ra View: "25/12/2026 15:30 (UTC+1)"
                        until = user.BannedUntil?.ToString("dd/MM/yyyy HH:mm") + " (" + offsetString + ")",
                        // Định dạng zzz sẽ tự động nối múi giờ vào đuôi (VD: 2026-12-25T15:30:00+01:00)
                        // Nhờ có +01:00, JS ở máy Việt Nam sẽ tự dịch ra giờ chuẩn xác để đếm ngược!
                        untilIso = user.BannedUntil?.ToString("yyyy-MM-ddTHH:mm:sszzz")
                    });
                }
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

            if (string.Equals(user.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        // TRANG HIỂN THỊ THÔNG BÁO LỖI KHI BỊ KHÓA NHẬN THÊM THAM SỐ `until`
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccountRestricted(string reason, string until, string untilIso) // Thêm tham số untilIso
        {
            ViewBag.Reason = reason ?? "Vi phạm chính sách và tiêu chuẩn cộng đồng của hệ thống.";
            ViewBag.BannedUntil = until;
            ViewBag.BannedUntilIso = untilIso; // Truyền ra View
            return View();
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
                // 1. Tạo Template HTML chuyên nghiệp
                string emailSubject = "Mã xác thực tài khoản RE:COLLECT";
                string emailBody = $@"
                <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px; background-color: #fcfcfc;'>
                    <div style='text-align: center; margin-bottom: 25px;'>
                        <h2 style='color: #2c3e50; margin: 0; font-size: 28px; letter-spacing: 1px;'>RE:COLLECT</h2>
                        <p style='color: #7f8c8d; font-size: 14px; margin-top: 5px;'>Hệ thống mua bán đồ thanh lý</p>
                    </div>
                    
                    <div style='background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); text-align: center; border-top: 4px solid #3498db;'>
                        <p style='font-size: 16px; color: #333; margin-bottom: 20px; text-align: left;'>Xin chào <strong>{model.FullName}</strong>,</p>
                        
                        <p style='font-size: 15px; color: #555; line-height: 1.6; text-align: left;'>
                            Cảm ơn bạn đã đăng ký tài khoản tại <strong>RE:COLLECT</strong>. Để hoàn tất quá trình đăng ký và bảo vệ tài khoản của bạn, vui lòng sử dụng mã xác nhận (OTP) gồm 6 chữ số dưới đây:
                        </p>
                        
                        <div style='margin: 35px 0;'>
                            <span style='font-size: 36px; font-weight: bold; color: #2980b9; letter-spacing: 8px; background-color: #ebf5fb; padding: 15px 30px; border-radius: 6px; border: 1px dashed #3498db; display: inline-block;'>
                                {verificationCode}
                            </span>
                        </div>
                        
                        <div style='background-color: #fdf2e9; border-left: 4px solid #e67e22; padding: 15px; margin-top: 25px; text-align: left; border-radius: 4px;'>
                            <p style='font-size: 14px; color: #d35400; margin: 0; line-height: 1.5;'>
                                <strong>Lưu ý bảo mật quan trọng:</strong><br/>
                                • Mã xác nhận này sẽ hết hạn trong vòng <strong>10 phút</strong>.<br/>
                                • Tuyệt đối không chia sẻ mã này cho bất kỳ ai, kể cả nhân viên RE:COLLECT.
                            </p>
                        </div>
                    </div>
                    
                    <div style='text-align: center; margin-top: 30px; color: #95a5a6; font-size: 12px; line-height: 1.5;'>
                        <p style='margin: 0;'>© {DateTime.Now.Year} RE:COLLECT. All rights reserved.</p>
                        <p style='margin: 5px 0 0 0;'>Đây là email gửi tự động từ hệ thống, vui lòng không trả lời email này.</p>
                    </div>
                </div>";

                // 2. Gửi email với HTML body
                await _emailService.SendEmailAsync(model.Email, emailSubject, emailBody);

                // 3. Lưu Session (như cũ)
                HttpContext.Session.SetString("VerificationCode", verificationCode);
                HttpContext.Session.SetString("PendingEmail", model.Email);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi gửi email: " + ex.Message);
                return View(model);
            }

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
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return RedirectToAction("Profile");
        }
        // --- 1. HIỂN THỊ FORM NHẬP EMAIL QUÊN MẬT KHẨU ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // --- 2. XỬ LÝ GỬI LINK QUA EMAIL ---
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập địa chỉ email.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại trong hệ thống.";
                return View();
            }

            // 1. Tạo chuỗi Token ngẫu nhiên
            string token = Guid.NewGuid().ToString();

            // 2. Lưu Token và Email vào Cache (Thời hạn 15 phút)
            var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
            await _cache.SetStringAsync(token, email, cacheOptions);

            // 3. Tạo đường dẫn đặt lại mật khẩu
            var resetLink = Url.Action("ResetPassword", "Account", new { token = token }, protocol: HttpContext.Request.Scheme);

            // 4. Gửi giao diện Email chuyên nghiệp
            string emailSubject = "Yêu cầu khôi phục mật khẩu RE:COLLECT";
            string emailBody = $@"
            <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px; background-color: #fcfcfc;'>
                <div style='text-align: center; margin-bottom: 25px;'>
                    <h2 style='color: #2c3e50; margin: 0; font-size: 28px; letter-spacing: 1px;'>RE:COLLECT</h2>
                    <p style='color: #7f8c8d; font-size: 14px; margin-top: 5px;'>Hệ thống mua bán đồ thanh lý</p>
                </div>
                
                <div style='background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); text-align: center; border-top: 4px solid #e74c3c;'>
                    <p style='font-size: 16px; color: #333; margin-bottom: 20px; text-align: left;'>Xin chào <strong>{user.FullName}</strong>,</p>
                    
                    <p style='font-size: 15px; color: #555; line-height: 1.6; text-align: left;'>
                        Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với email <strong>{email}</strong>. 
                        Vui lòng nhấn vào nút bên dưới để tiến hành đổi mật khẩu mới:
                    </p>
                    
                    <div style='margin: 35px 0;'>
                        <a href='{resetLink}' style='background-color: #e74c3c; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px; display: inline-block;'>ĐẶT LẠI MẬT KHẨU</a>
                    </div>
                    
                    <div style='background-color: #fdf2e9; border-left: 4px solid #e67e22; padding: 15px; margin-top: 25px; text-align: left; border-radius: 4px;'>
                        <p style='font-size: 14px; color: #d35400; margin: 0; line-height: 1.5;'>
                            <strong>Lưu ý:</strong><br/>
                            • Đường dẫn này sẽ hết hạn trong vòng <strong>15 phút</strong>.<br/>
                            • Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.
                        </p>
                    </div>
                </div>
            </div>";

            try
            {
                await _emailService.SendEmailAsync(email, emailSubject, emailBody);
                ViewBag.Success = "Đường dẫn khôi phục mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra khi gửi email: " + ex.Message;
            }

            return View();
        }

        // --- 3. HIỂN THỊ FORM ĐỔI MẬT KHẨU MỚI (Từ link Email) ---
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login");

            var email = await _cache.GetStringAsync(token);
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Đường dẫn đặt lại mật khẩu đã hết hạn hoặc không hợp lệ. Vui lòng yêu cầu lại.";
                return View();
            }

            // Trừu tượng hóa bằng ViewModel
            var model = new ResetPasswordVM { Token = token };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Bắt lỗi chữ đỏ dưới ô input nếu không hợp lệ
            }

            var email = await _cache.GetStringAsync(model.Token);
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Đường dẫn đặt lại mật khẩu đã hết hạn hoặc không hợp lệ.";
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, model.NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                await _cache.RemoveAsync(model.Token);

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại với mật khẩu mới.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "Không tìm thấy người dùng.";
            return View(model);
        }
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBH.Models;

namespace WebBH.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(WebThanhLyDbContext context)
        {
            await context.Database.MigrateAsync();

            // 1. Đảm bảo có Roles trước
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { Name = "Admin" },
                    new Role { Name = "User" }
                );
                await context.SaveChangesAsync();
            }

            // 2. Lấy Role IDs
            var adminRole = context.Roles.First(r => r.Name == "Admin");
            var userRole = context.Roles.First(r => r.Name == "User");
            var hasher = new PasswordHasher<User>();

            // 3. Nạp Admin cụ thể (Check theo Email để tránh lỗi bảng không trống)
            if (!context.Users.Any(u => u.Email == "duyvan921@gmail.com"))
            {
                var adminNew = new User
                {
                    Email = "duyvan921@gmail.com", // Sửa lại .com chuẩn
                    FullName = "Đỗ Duy Văn",
                    PhoneNumber = "0912345678",
                    RoleId = adminRole.RoleId,
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };
                adminNew.PasswordHash = hasher.HashPassword(adminNew, "Vanichikak6");
                context.Users.Add(adminNew);
            }

            // 4. Nạp các User mẫu khác nếu chưa có bất kỳ ai
            if (!context.Users.Any(u => u.Email == "admin@webthanhly.com"))
            {
                var adminLegacy = new User
                {
                    Email = "admin@webthanhly.com",
                    FullName = "System Admin",
                    RoleId = adminRole.RoleId,
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };
                adminLegacy.PasswordHash = hasher.HashPassword(adminLegacy, "Admin@123");
                context.Users.Add(adminLegacy);
            }

            // Lưu tất cả thay đổi
            await context.SaveChangesAsync();
        }
    }
}
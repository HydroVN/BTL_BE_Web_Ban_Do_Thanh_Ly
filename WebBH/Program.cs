using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebBH.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Giữ nguyên cấu hình Database của bạn
builder.Services.AddDbContext<WebThanhLyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBWeb")));
builder.Services.AddDistributedMemoryCache();
// 1. THÊM DỊCH VỤ SESSION (Cần thiết cho xác thực Email)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10); // Mã xác nhận có hiệu lực trong 10 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Giữ nguyên cấu hình Authentication của bạn
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";               // Đường dẫn trang đăng nhập
        options.LogoutPath = "/Account/Logout";             // Đường dẫn trang đăng xuất
        options.AccessDeniedPath = "/Account/AccessDenied";  // Trang khi không đủ quyền
        options.ExpireTimeSpan = TimeSpan.FromHours(1);      // Thời gian sống của Cookie
    });
builder.Services.AddTransient<WebBH.Services.EmailService>();
var app = builder.Build();
<<<<<<< HEAD
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var context = services.GetRequiredService<WebThanhLyDbContext>();

//    await SeedData.InitializeAsync(context);
//}
=======

>>>>>>> origin/main
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//// Khởi tạo dữ liệu mẫu (Seed Data)
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<WebThanhLyDbContext>();
//    await SeedData.InitializeAsync(db);
//}

app.UseHttpsRedirection();
app.UseRouting();

// 2. KÍCH HOẠT SESSION (Bắt buộc phải nằm TRƯỚC Authentication)
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Giữ nguyên các định tuyến của bạn
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
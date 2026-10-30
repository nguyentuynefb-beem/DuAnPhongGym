using Microsoft.EntityFrameworkCore;
using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using DuAnCuaToi.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký CSDL SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddHttpClient<IGeminiService, GeminiService>(client =>
    {
        // Tránh request treo quá lâu khi Gemini/API network gặp sự cố.
        client.Timeout = TimeSpan.FromSeconds(20);
    });
// Health service centralizes health/profile logic
builder.Services.AddScoped<IHealthService, HealthService>();

// AI Context Builder trích xuất từ UserController
builder.Services.AddScoped<IAIContextBuilder, AIContextBuilder>();

// Dịch vụ tổng hợp báo cáo doanh thu (đọc từ CSDL) + xuất file DOCX/PDF
builder.Services.AddScoped<IRevenueReportService, RevenueReportService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();

// Cấu hình license miễn phí (Community) cho thư viện xuất PDF QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Cấu hình Session (duy trì để tương thích giao diện Razor)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình CookieAuthentication chuẩn của ASP.NET Core
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.AccessDeniedPath = "/Home/Index";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

var app = builder.Build();

// 3. Tự động Migration & bảo đảm tài khoản quản trị mặc định hợp lệ
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    const string adminUsername = "AdminGym";
    const string adminPassword = "tuyen123456";

    // Không tạo dữ liệu người dùng mẫu ngoài yêu cầu. Nếu AdminGym đã tồn tại
    // thì chuẩn hóa lại role + mật khẩu để tránh dữ liệu cũ/plain-text làm lỗi đăng nhập.
    var admin = dbContext.NguoiDungs
        .FirstOrDefault(u => u.TenDangNhap == adminUsername);

    if (admin == null)
    {
        admin = new NguoiDung
        {
            TenDangNhap = adminUsername,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            HoTen = "Quản Trị Viên Hệ Thống",
            VaiTro = "Admin",
            IsOnline = false
        };

        dbContext.NguoiDungs.Add(admin);
    }
    else
    {
        admin.HoTen = string.IsNullOrWhiteSpace(admin.HoTen)
            ? "Quản Trị Viên Hệ Thống"
            : admin.HoTen;
        admin.VaiTro = "Admin";
        
        // Giữ nguyên mật khẩu hiện tại nếu đã có, chỉ cấp mật khẩu mặc định nếu hash trống
        if (string.IsNullOrWhiteSpace(admin.MatKhauHash))
        {
            admin.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        }

        admin.IsOnline = false;
    }

    dbContext.SaveChanges();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Kích hoạt Middleware Session & Authentication (Đặt sau UseRouting và trước UseAuthorization/MapControllerRoute)
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
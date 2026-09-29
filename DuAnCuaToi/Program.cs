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
        client.Timeout = TimeSpan.FromSeconds(60);
    });
// Health service centralizes health/profile logic
builder.Services.AddScoped<IHealthService, HealthService>();

// Dịch vụ tổng hợp báo cáo doanh thu (đọc từ CSDL) + xuất file DOCX/PDF
builder.Services.AddScoped<IRevenueReportService, RevenueReportService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();

// Cấu hình license miễn phí (Community) cho thư viện xuất PDF QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian hết hạn phiên
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 3. Tự động Migration & Seed Tài khoản AdminGym thật vào SQL Server
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate(); // Tự tạo CSDL nếu chưa có
                                  // Thêm đoạn này trong khối seed Data của Program.cs
    if (!dbContext.NguoiDungs.Any(u => u.VaiTro == "PT"))
    {
        dbContext.NguoiDungs.AddRange(
            new NguoiDung { TenDangNhap = "pt_nguyenvanc", MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456"), HoTen = "Nguyễn Văn C (Gym & Fitness)", VaiTro = "PT" },
            new NguoiDung { TenDangNhap = "pt_lethid", MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456"), HoTen = "Lê Thị D (Yoga & Cardio)", VaiTro = "PT" },
            new NguoiDung { TenDangNhap = "pt_tranvane", MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456"), HoTen = "Trần Văn E (Boxing & Kickfit)", VaiTro = "PT" }
        );
        dbContext.SaveChanges();
    }

    if (!dbContext.NguoiDungs.Any(u => u.TenDangNhap == "AdminGym"))
    {
        dbContext.NguoiDungs.Add(new NguoiDung
        {
            TenDangNhap = "AdminGym",
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456"), // Mã hóa mật khẩu thật
            HoTen = "Quản Trị Viên Hệ Thống",
            VaiTro = "Admin"
        });
        dbContext.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Kích hoạt Middleware Session (Đặt sau UseRouting và trước UseAuthorization/MapControllerRoute)
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
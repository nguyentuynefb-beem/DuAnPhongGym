using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DuAnCuaToi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // TRANG ĐĂNG NHẬP
        // =========================================================
        [HttpGet]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("Role");

            // Phục hồi session nếu còn Cookie xác thực hợp lệ
            if ((string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role)) && User.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(role))
                {
                    HttpContext.Session.SetString("UserId", userId);
                    HttpContext.Session.SetString("Role", role);
                    HttpContext.Session.SetString("Username", User.FindFirst(ClaimTypes.Name)?.Value ?? "");
                    HttpContext.Session.SetString("UserName", User.FindFirst("FullName")?.Value ?? "");
                }
            }

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(role))
            {
                return role switch
                {
                    "Admin" or "AdminGym" => RedirectToAction("Index", "Admin"),
                    "PT" => RedirectToAction("Index", "PT"),
                    "LeTan" => RedirectToAction("Index", "LeTan"),
                    "HoiVien" => RedirectToAction("Index", "User"),
                    _ => View(new LoginViewModel())
                };
            }

            return View(new LoginViewModel());
        }

        // =========================================================
        // TRANG CHÍNH SÁCH BẢO MẬT
        // =========================================================
        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        // =========================================================
        // XỬ LÝ ĐĂNG NHẬP
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var username = model.Username.Trim();

            // Tìm tài khoản theo tên đăng nhập (chỉ tài khoản chưa bị xóa).
            var user = await _context.NguoiDungs
                .FirstOrDefaultAsync(u => u.TenDangNhap == username && !u.IsDeleted);

            var passwordValid = false;
            if (user != null && !string.IsNullOrWhiteSpace(user.MatKhauHash))
            {
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhauHash);
                }
                catch
                {
                    // Tự động nâng cấp sang mã hóa BCrypt an toàn nếu dữ liệu cũ từng lưu mật khẩu thô
                    if (model.Password == user.MatKhauHash)
                    {
                        passwordValid = true;
                        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    }
                    else
                    {
                        passwordValid = false;
                    }
                }
            }

            if (user == null || !passwordValid)
            {
                ModelState.AddModelError(
                    "",
                    "Tên đăng nhập hoặc mật khẩu không chính xác!"
                );

                return View("Index", model);
            }

            // =====================================================
            // CẬP NHẬT TRẠNG THÁI ONLINE
            // =====================================================
            user.IsOnline = true;
            user.LastLogin = DateTime.Now;

            await _context.SaveChangesAsync();

            // =====================================================
            // DUAL-MODE 1: LƯU THÔNG TIN VÀO SESSION
            // Để tương thích 100% với các View Razor đang dùng Context.Session
            // =====================================================
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Role", user.VaiTro ?? "");
            HttpContext.Session.SetString("UserName", user.HoTen ?? "");
            HttpContext.Session.SetString("Username", user.TenDangNhap ?? "");

            // =====================================================
            // DUAL-MODE 2: COOKIE AUTHENTICATION CHUẨN ASP.NET CORE
            // Giữ phiên đăng nhập an toàn, bền bỉ qua cookie
            // =====================================================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim("FullName", user.HoTen ?? ""),
                new Claim(ClaimTypes.Role, user.VaiTro ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // =====================================================
            // CHUYỂN TRANG THEO VAI TRÒ
            // =====================================================
            switch (user.VaiTro)
            {
                case "AdminGym":
                case "Admin":
                    return RedirectToAction(
                        "Index",
                        "Admin"
                    );

                case "PT":
                    return RedirectToAction(
                        "Index",
                        "PT"
                    );

                case "LeTan":
                    return RedirectToAction(
                        "Index",
                        "LeTan"
                    );

                case "HoiVien":
                    return RedirectToAction(
                        "Index",
                        "User"
                    );

                default:
                    user.IsOnline = false;
                    await _context.SaveChangesAsync();
                    HttpContext.Session.Clear();
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    ModelState.AddModelError("", "Tài khoản có vai trò không hợp lệ. Vui lòng liên hệ quản trị viên.");
                    return View("Index", model);
            }
        }

        // =========================================================
        // ĐĂNG XUẤT
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Lấy UserId từ Session
            var userIdString = HttpContext.Session.GetString("UserId");

            // Nếu có UserId thì cập nhật trạng thái Offline
            if (!string.IsNullOrEmpty(userIdString) &&
                int.TryParse(userIdString, out int userId))
            {
                var user = await _context.NguoiDungs.FindAsync(userId);

                if (user != null)
                {
                    user.IsOnline = false;
                    await _context.SaveChangesAsync();
                }
            }

            // Xóa Session và thu hồi Cookie đăng nhập
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Quay về trang đăng nhập
            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        // =========================================================
        // ĐĂNG KÝ TÀI KHOẢN HỘI VIÊN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string username,
            string password,
            string fullName)
        {
            // =====================================================
            // KIỂM TRA DỮ LIỆU
            // =====================================================
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] =
                    "Vui lòng điền đầy đủ thông tin!";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // LOẠI BỎ KHOẢNG TRẮNG THỪA & KIỂM TRA ĐỘ MẠNH MẬT KHẨU
            // =====================================================
            username = username.Trim();
            fullName = fullName.Trim();

            if (password.Length < 6)
            {
                TempData["Error"] =
                    "Mật khẩu phải có ít nhất 6 ký tự để đảm bảo an toàn!";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // KIỂM TRA TÊN ĐĂNG NHẬP ĐÃ TỒN TẠI CHƯA (trên các tài khoản đang hoạt động)
            // =====================================================
            bool exists = await _context.NguoiDungs
                .AnyAsync(u => u.TenDangNhap == username && !u.IsDeleted);

            if (exists)
            {
                TempData["Error"] =
                    "Tên đăng nhập đã được sử dụng!";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // TẠO TÀI KHOẢN HỘI VIÊN
            // =====================================================
            var newUser = new NguoiDung
            {
                TenDangNhap = username,

                // Mã hóa mật khẩu bằng BCrypt
                MatKhauHash =
                    BCrypt.Net.BCrypt.HashPassword(password),

                HoTen = fullName,

                // Tài khoản đăng ký mới mặc định là Hội viên
                VaiTro = "HoiVien",

                // Chưa có Id nên tạm để null
                MaHoiVien = null,

                // Trạng thái ban đầu
                IsOnline = false,
                LastLogin = null,

                // Thông tin thể chất ban đầu
                ChieuCao = 0,
                CanNang = 0,

                // Mục tiêu mặc định
                MucTieuTapLuyen = "Mới tham gia",

                GhiChuSucKhoe = null
            };

            // =====================================================
            // LƯU USER LẦN 1
            // Để DATABASE sinh Id
            // =====================================================
            _context.NguoiDungs.Add(newUser);

            await _context.SaveChangesAsync();

            // =====================================================
            // SAU SaveChanges(), newUser.Id đã có giá trị
            // =====================================================
            newUser.MaHoiVien =
                $"HV{newUser.Id:0000}";

            // Lưu mã hội viên
            await _context.SaveChangesAsync();

            // =====================================================
            // THÔNG BÁO ĐĂNG KÝ THÀNH CÔNG
            // =====================================================
            TempData["Success"] =
                $"Đăng ký thành công! Mã hội viên của bạn là {newUser.MaHoiVien}. Vui lòng đăng nhập.";

            return RedirectToAction(nameof(Index));
        }
    }
}
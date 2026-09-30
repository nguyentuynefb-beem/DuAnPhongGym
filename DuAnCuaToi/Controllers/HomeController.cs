using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using Microsoft.AspNetCore.Mvc;

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
            return View(new LoginViewModel());
        }

        // =========================================================
        // XỬ LÝ ĐĂNG NHẬP
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var username = model.Username.Trim();

            // Tìm tài khoản theo tên đăng nhập.
            var user = _context.NguoiDungs
                .FirstOrDefault(u => u.TenDangNhap == username);

            // BCrypt.Verify sẽ ném lỗi nếu dữ liệu legacy từng lưu mật khẩu thô
            // trong cột MatKhauHash. Coi hash sai định dạng là đăng nhập thất bại.
            var passwordValid = false;
            if (user != null && !string.IsNullOrWhiteSpace(user.MatKhauHash))
            {
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhauHash);
                }
                catch
                {
                    passwordValid = false;
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

            _context.SaveChanges();

            // =====================================================
            // LƯU THÔNG TIN NGƯỜI DÙNG VÀO SESSION
            //
            // QUAN TRỌNG:
            // UserId đang được lưu bằng SetString()
            // => UserController cũng phải dùng GetString()
            // =====================================================
            HttpContext.Session.SetString(
                "UserId",
                user.Id.ToString()
            );

            HttpContext.Session.SetString(
                "Role",
                user.VaiTro ?? ""
            );

            HttpContext.Session.SetString(
                "UserName",
                user.HoTen ?? ""
            );

            HttpContext.Session.SetString(
                "Username",
                user.TenDangNhap ?? ""
            );

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
                    _context.SaveChanges();
                    HttpContext.Session.Clear();
                    ModelState.AddModelError("", "Tài khoản có vai trò không hợp lệ. Vui lòng liên hệ quản trị viên.");
                    return View("Index", model);
            }
        }

        // =========================================================
        // ĐĂNG XUẤT
        // =========================================================
        [HttpGet]
        public IActionResult Logout()
        {
            // Lấy UserId từ Session
            var userIdString = HttpContext.Session.GetString("UserId");

            // Nếu có UserId thì cập nhật trạng thái Offline
            if (!string.IsNullOrEmpty(userIdString) &&
                int.TryParse(userIdString, out int userId))
            {
                var user = _context.NguoiDungs
                    .Find(userId);

                if (user != null)
                {
                    user.IsOnline = false;

                    _context.SaveChanges();
                }
            }

            // Xóa toàn bộ Session
            HttpContext.Session.Clear();

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
        public IActionResult Register(
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
            // LOẠI BỎ KHOẢNG TRẮNG THỪA
            // =====================================================
            username = username.Trim();
            fullName = fullName.Trim();

            // =====================================================
            // KIỂM TRA TÊN ĐĂNG NHẬP ĐÃ TỒN TẠI CHƯA
            // =====================================================
            bool exists = _context.NguoiDungs
                .Any(u => u.TenDangNhap == username);

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

            _context.SaveChanges();

            // =====================================================
            // SAU SaveChanges(), newUser.Id đã có giá trị
            //
            // Ví dụ:
            // Id = 5
            // => MaHoiVien = HV0005
            // =====================================================
            newUser.MaHoiVien =
                $"HV{newUser.Id:0000}";

            // Lưu mã hội viên
            _context.SaveChanges();

            // =====================================================
            // THÔNG BÁO ĐĂNG KÝ THÀNH CÔNG
            // =====================================================
            TempData["Success"] =
                $"Đăng ký thành công! Mã hội viên của bạn là {newUser.MaHoiVien}. Vui lòng đăng nhập.";

            return RedirectToAction(nameof(Index));
        }
    }
}
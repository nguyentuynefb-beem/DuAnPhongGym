using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using DuAnCuaToi.Filters;
using DuAnCuaToi.Services;

namespace DuAnCuaToi.Controllers
{
    [SessionRole("Admin", "AdminGym")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly IAIContextBuilder _aiContextBuilder;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IGeminiService geminiService,
            IAIContextBuilder aiContextBuilder,
            ILogger<AdminController> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _aiContextBuilder = aiContextBuilder;
            _logger = logger;
        }

        // 1. Lấy toàn bộ tài khoản trong bảng NguoiDungs (không dùng .Where)
        public async Task<IActionResult> Index(string? search)
        {
            // Query người dùng (chỉ lấy tài khoản chưa bị xóa)
            var users = _context.NguoiDungs.Where(u => !u.IsDeleted).AsQueryable();

            // Query gói tập (chỉ lấy gói chưa bị xóa)
            var goiTaps = _context.GoiTaps
                .Where(g => !g.IsDeleted)
                .Include(g => g.GoiTapDichVus)
                .ThenInclude(gd => gd.DichVu)
                .AsQueryable();

            // Nếu có nhập từ khóa
            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u =>
                    u.HoTen.Contains(search) ||
                    u.TenDangNhap.Contains(search));

                goiTaps = goiTaps.Where(g =>
                    g.TenGoi.Contains(search) ||
                    g.MaGoi.Contains(search));
            }

            ViewBag.Search = search;
            ViewBag.GoiTaps = await goiTaps.ToListAsync();
            ViewBag.DichVus = await _context.DichVus.ToListAsync();

            return View(await users.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGoiTap(
            string maGoi,
            string tenGoi,
            int thoiHanNgay,
            decimal gia,
            string[] quyenLoiList, // Nhận mảng danh sách quyền lợi được tích chọn
            bool isKhuyenMai,
            int[] selectedDichVus)
        {
            if (await _context.GoiTaps.AnyAsync(g => g.MaGoi == maGoi && !g.IsDeleted))
            {
                TempData["Error"] = "Mã gói tập đã tồn tại!";
                return RedirectToAction("Index");
            }

            // Tự động gộp danh sách tích chọn thành chuỗi phân cách bởi dấu phẩy
            string chuoiQuyenLoi = (quyenLoiList != null && quyenLoiList.Length > 0)
                ? string.Join(", ", quyenLoiList)
                : "Tập tự do";

            var goiTap = new GoiTap
            {
                MaGoi = maGoi,
                TenGoi = tenGoi,
                ThoiHanNgay = thoiHanNgay,
                Gia = gia,
                QuyenLoi = chuoiQuyenLoi,
                IsKhuyenMai = isKhuyenMai
            };

            _context.GoiTaps.Add(goiTap);
            await _context.SaveChangesAsync();

            if (selectedDichVus != null && selectedDichVus.Length > 0)
            {
                foreach (var dichVuId in selectedDichVus)
                {
                    _context.GoiTapDichVus.Add(new GoiTapDichVu
                    {
                        GoiTapId = goiTap.Id,
                        DichVuId = dichVuId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Thêm gói tập mới thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGoiTap(string maGoi, string tenGoi, int thoiHanNgay, decimal gia, string quyenLoi, List<string> quyenLoiList)
        {
            var goiTap = await _context.GoiTaps
                .Include(g => g.GoiTapDichVus)
                .FirstOrDefaultAsync(g => g.MaGoi == maGoi);

            if (goiTap != null)
            {
                goiTap.TenGoi = tenGoi;
                goiTap.ThoiHanNgay = thoiHanNgay;
                goiTap.Gia = gia;

                // Đồng bộ quyền lợi chuỗi
                string chuoiQuyenLoi = (quyenLoiList != null && quyenLoiList.Count > 0)
                    ? string.Join(", ", quyenLoiList)
                    : (!string.IsNullOrWhiteSpace(quyenLoi) ? quyenLoi : "Tập tự do");
                goiTap.QuyenLoi = chuoiQuyenLoi;

                // Đồng bộ lại các dịch vụ đi kèm trong bảng trung gian GoiTapDichVu
                _context.GoiTapDichVus.RemoveRange(goiTap.GoiTapDichVus);

                if (quyenLoiList != null && quyenLoiList.Count > 0)
                {
                    var allDichVus = await _context.DichVus.ToListAsync();
                    foreach (var tenDv in quyenLoiList)
                    {
                        var dv = allDichVus.FirstOrDefault(d => 
                            d.TenDichVu.Equals(tenDv, StringComparison.OrdinalIgnoreCase) ||
                            d.TenDichVu.Contains(tenDv, StringComparison.OrdinalIgnoreCase));

                        if (dv != null)
                        {
                            _context.GoiTapDichVus.Add(new GoiTapDichVu
                            {
                                GoiTapId = goiTap.Id,
                                DichVuId = dv.Id
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật thông tin gói tập {maGoi} thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy gói tập cần sửa!";
            }
            return RedirectToAction("Index");
        }

        // 2. XÓA GÓI TẬP (Soft Delete - Bảo vệ toàn vẹn dữ liệu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGoiTap(string maGoi)
        {
            var goiTap = await _context.GoiTaps.FirstOrDefaultAsync(g => g.MaGoi == maGoi);
            if (goiTap != null)
            {
                // Đánh dấu xóa mềm, không xóa vật lý để tránh lỗi Foreign Key Restrict
                goiTap.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa gói tập {maGoi} thành công!";
            }
            else
            {
                TempData["Error"] = "Gói tập không tồn tại hoặc đã bị xóa trước đó!";
            }
            return RedirectToAction("Index");
        }

        // 2. Action cập nhật vai trò
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(int userId, string newRole)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản!";
                return RedirectToAction("Index");
            }

            newRole = (newRole ?? string.Empty).Trim();
            if (newRole == "AdminGym")
                newRole = "Admin";

            var allowedRoles = new[] { "Admin", "PT", "LeTan", "HoiVien" };
            if (!allowedRoles.Contains(newRole))
            {
                TempData["Error"] = "Vai trò không hợp lệ!";
                return RedirectToAction("Index");
            }

            // Tài khoản quản trị mặc định phải luôn giữ quyền Admin.
            if (user.TenDangNhap == "AdminGym" && newRole != "Admin")
            {
                TempData["Error"] = "Không thể hạ quyền tài khoản AdminGym mặc định.";
                return RedirectToAction("Index");
            }

            // Ngăn chặn Admin tự hạ quyền của chính mình khi đang đăng nhập
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (currentUserId == userId.ToString() && newRole != "Admin")
            {
                TempData["Error"] = "Bạn không thể tự hạ quyền Admin của chính mình khi đang đăng nhập!";
                return RedirectToAction("Index");
            }

            user.VaiTro = newRole;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật vai trò cho [{user.HoTen}] thành [{newRole}]!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _context.NguoiDungs
                               .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("Index");
            }

            if (user.TenDangNhap == "AdminGym")
            {
                TempData["Error"] = "Không thể xóa tài khoản AdminGym mặc định!";
                return RedirectToAction("Index");
            }

            // Ngăn chặn Admin tự xóa tài khoản của chính mình khi đang đăng nhập
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (currentUserId == userId.ToString())
            {
                TempData["Error"] = "Bạn không thể tự xóa tài khoản đang đăng nhập của chính mình!";
                return RedirectToAction("Index");
            }

            // Đánh dấu xóa mềm, không xóa vật lý để tránh lỗi Foreign Key Restrict
            user.IsDeleted = true;
            user.IsOnline = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa người dùng [{user.HoTen}] thành công!";

            return RedirectToAction("Index");
        }

        // 3. ĐẶT LẠI MẬT KHẨU CHO NGƯỜI DÙNG (Admin Reset Password)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int userId, string? newPassword)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản người dùng!";
                return RedirectToAction("Index");
            }

            var passToSet = string.IsNullOrWhiteSpace(newPassword) ? "123456" : newPassword.Trim();
            if (passToSet.Length < 6)
            {
                TempData["Error"] = "Mật khẩu đặt lại phải có ít nhất 6 ký tự!";
                return RedirectToAction("Index");
            }

            user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(passToSet);
            user.IsOnline = false; // Thu hồi trạng thái online
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã đặt lại mật khẩu cho tài khoản [{user.HoTen}] ({user.TenDangNhap}) thành công! Mật khẩu mới: {passToSet}";
            return RedirectToAction("Index");
        }

        // 4. TẠO TÀI KHOẢN MỚI THEO VAI TRÒ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMember(
            string username,
            string password,
            string fullName,
            string? role,
            string? goal,
            string? timeAvailable)
        {
            username = (username ?? string.Empty).Trim();
            fullName = (fullName ?? string.Empty).Trim();
            password ??= string.Empty;
            role = (role ?? "HoiVien").Trim();

            var allowedRoles = new[] { "Admin", "PT", "LeTan", "HoiVien" };
            if (!allowedRoles.Contains(role))
            {
                role = "HoiVien";
            }

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(fullName) ||
                password.Length < 6)
            {
                TempData["Error"] = "Tên đăng nhập, họ tên là bắt buộc và mật khẩu phải có ít nhất 6 ký tự!";
                return RedirectToAction("Index");
            }

            // Kiểm tra tên đăng nhập đã tồn tại chưa
            if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == username && !u.IsDeleted))
            {
                TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                return RedirectToAction("Index");
            }

            var user = new NguoiDung
            {
                TenDangNhap = username,
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
                HoTen = fullName,
                VaiTro = role,
                MucTieuTapLuyen = goal,
                GhiChuSucKhoe = timeAvailable
            };

            _context.NguoiDungs.Add(user);
            await _context.SaveChangesAsync();

            // Nếu là Hội viên thì sinh mã hội viên
            if (role == "HoiVien")
            {
                user.MaHoiVien = $"HV{user.Id:0000}";
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Đã tạo tài khoản [{fullName}] với vai trò [{role}] thành công!";
            return RedirectToAction("Index");
        }

        // =========================================================
        // THÔNG TIN CÁ NHÂN & ĐỔI MẬT KHẨU ADMIN
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out int adminId))
            {
                return RedirectToAction("Index", "Home");
            }

            var admin = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Id == adminId);
            if (admin == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new EditProfileViewModel
            {
                Id = admin.Id,
                HoTen = admin.HoTen,
                TenDangNhap = admin.TenDangNhap
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out int adminId) || adminId != model.Id)
            {
                return Forbid();
            }

            var admin = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Id == model.Id && (u.VaiTro == "Admin" || u.VaiTro == "AdminGym"));
            if (admin == null)
            {
                return RedirectToAction("Index", "Home");
            }

            model.HoTen = model.HoTen.Trim();
            model.TenDangNhap = model.TenDangNhap.Trim();

            var usernameExists = await _context.NguoiDungs.AnyAsync(x => x.Id != admin.Id && x.TenDangNhap == model.TenDangNhap);
            if (usernameExists)
            {
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã được sử dụng.");
                return View(model);
            }

            admin.HoTen = model.HoTen;
            admin.TenDangNhap = model.TenDangNhap;

            // Xử lý đổi mật khẩu an toàn
            if (!string.IsNullOrWhiteSpace(model.MatKhauMoi))
            {
                if (string.IsNullOrWhiteSpace(model.MatKhauHienTai))
                {
                    ModelState.AddModelError(nameof(model.MatKhauHienTai), "Vui lòng nhập mật khẩu hiện tại để xác thực đổi mật khẩu.");
                    return View(model);
                }

                bool isOldPasswordCorrect = false;
                try
                {
                    isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(model.MatKhauHienTai, admin.MatKhauHash);
                }
                catch
                {
                    isOldPasswordCorrect = (model.MatKhauHienTai == admin.MatKhauHash);
                }

                if (!isOldPasswordCorrect)
                {
                    ModelState.AddModelError(nameof(model.MatKhauHienTai), "Mật khẩu hiện tại không chính xác.");
                    return View(model);
                }

                if (model.MatKhauMoi.Length < 6)
                {
                    ModelState.AddModelError(nameof(model.MatKhauMoi), "Mật khẩu mới phải có ít nhất 6 ký tự.");
                    return View(model);
                }

                if (model.MatKhauMoi == model.MatKhauHienTai)
                {
                    ModelState.AddModelError(nameof(model.MatKhauMoi), "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
                    return View(model);
                }

                if (model.MatKhauMoi != model.XacNhanMatKhau)
                {
                    ModelState.AddModelError(nameof(model.XacNhanMatKhau), "Xác nhận mật khẩu mới không khớp.");
                    return View(model);
                }

                admin.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.MatKhauMoi);
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("Username", admin.TenDangNhap);
            HttpContext.Session.SetString("UserName", admin.HoTen);

            TempData["Success"] = "Cập nhật thông tin quản trị viên thành công!";
            return RedirectToAction("Index");
        }

        // =========================================================
        // TRỢ LÝ AI CHO QUẢN LÝ / ADMIN (EXECUTIVE AI)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> TroLyAI()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfToday = now.Date;
            var endOfToday = startOfToday.AddDays(1).AddTicks(-1);

            // Thống kê nhanh hiển thị trên header trang AI
            var doanhThuThang = await _context.HoaDons
                .Where(x => (x.TrangThai == "Đã thanh toán" || x.TrangThai == "DaThanhToan") &&
                            x.NgayLap >= startOfMonth)
                .SumAsync(x => (decimal?)x.TongTien) ?? 0;

            var tongHoiVien = await _context.NguoiDungs
                .CountAsync(x => !x.IsDeleted && x.VaiTro == "HoiVien");

            var checkInHomNay = await _context.CheckIns
                .CountAsync(x => x.ThoiGian >= startOfToday && x.ThoiGian <= endOfToday);

            var goiTapHoatDong = await _context.DangKyGoiTaps
                .CountAsync(x => x.TrangThai && x.NgayHetHan >= startOfToday);

            ViewBag.DoanhThuThang = doanhThuThang;
            ViewBag.TongHoiVien = tongHoiVien;
            ViewBag.CheckInHomNay = checkInHomNay;
            ViewBag.GoiTapHoatDong = goiTapHoatDong;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> AiChatAdmin([FromBody] AIChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng nhập câu hỏi hoặc yêu cầu phân tích."
                });
            }

            var question = request.Question.Trim();
            if (question.Length > 1200)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Nội dung câu hỏi không được vượt quá 1200 ký tự."
                });
            }

            try
            {
                // Tổng hợp context toàn diện của hệ thống dành cho Admin
                var adminContext = _aiContextBuilder.BuildAdminSystemContext(question);
                var adminContextJson = Newtonsoft.Json.JsonConvert.SerializeObject(adminContext);

                // Lịch sử chat (lấy 6 tin gần nhất)
                var history = (request.History ?? new List<AIChatHistoryItem>())
                    .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                    .TakeLast(6)
                    .Select(x =>
                    {
                        var role = x.Role == "user" ? "Admin" : "SmartGym AI";
                        var content = x.Content?.Trim() ?? "";
                        if (content.Length > 500) content = content[..500];
                        return $"{role}: {content}";
                    })
                    .ToList();

                var historyText = history.Count == 0
                    ? "Chưa có lịch sử trò chuyện."
                    : string.Join("\n", history);

                // Gọi Gemini AI với System Prompt quản trị cao cấp
                var answer = await _geminiService.ChatWithAdminAsync(question, adminContextJson, historyText);

                return Json(new
                {
                    success = true,
                    data = answer
                });
            }
            catch (GeminiServiceException ex)
            {
                _logger.LogWarning(ex, "Gemini service exception in AiChatAdmin: {Message}", ex.Message);
                return StatusCode(ex.HttpStatusCode, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AiChatAdmin");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Trợ lý AI đang bận xử lý hoặc gặp sự cố kết nối. Vui lòng thử lại sau ít phút."
                });
            }
        }
    }
}

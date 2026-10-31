using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using DuAnCuaToi.Services;
using DuAnCuaToi.Filters;
using DuAnCuaToi.ViewModels;

namespace DuAnCuaToi.Controllers
{
    [SessionRole("PT")]
    public class PTController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHealthService _healthService;

        public PTController(ApplicationDbContext context, IHealthService healthService)
        {
            _context = context;
            _healthService = healthService;
        }

        // =========================================================
        // LẤY ID PT HIỆN TẠI TỪ SESSION
        // HomeController lưu UserId bằng SetString()
        // nên PTController phải dùng GetString() + TryParse()
        // =========================================================
        private int? GetCurrentPtId()
        {
            var userIdString =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userIdString) ||
                !int.TryParse(userIdString, out int userId))
            {
                return null;
            }

            return userId;
        }

        // =========================================================
        // LẤY THÔNG TIN PT HIỆN TẠI
        // =========================================================
        private async Task<NguoiDung?> GetCurrentPtAsync()
        {
            var userId = GetCurrentPtId();

            if (userId == null)
            {
                return null;
            }

            return await _context.NguoiDungs
                .FirstOrDefaultAsync(x =>
                    x.Id == userId.Value &&
                    x.VaiTro == "PT");
        }

        // =========================================================
        // DASHBOARD PT
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(int? selectedMemberId)
        {
            var userId = GetCurrentPtId();

            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // =====================================================
            // KIỂM TRA TÀI KHOẢN PT
            // =====================================================
            var pt = await _context.NguoiDungs
                .FirstOrDefaultAsync(x =>
                    x.Id == userId.Value &&
                    x.VaiTro == "PT");

            if (pt == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            // =====================================================
            // USERNAME PT
            // =====================================================
            var ptUsername =
                HttpContext.Session.GetString("Username");

            if (string.IsNullOrWhiteSpace(ptUsername))
            {
                ptUsername = pt.TenDangNhap;

                HttpContext.Session.SetString(
                    "Username",
                    pt.TenDangNhap ?? "");
            }

            var today = DateTime.Today;

            // =====================================================
            // 1. THÔNG BÁO CỦA PT
            // =====================================================
            var thongBao = await _context.ThongBaos
                .Where(x =>
                    x.NguoiNhanId == userId.Value)
                .OrderByDescending(x => x.NgayTao)
                .ToListAsync();

            ViewBag.ThongBao = thongBao;

            ViewBag.SoThongBao =
                thongBao.Count(x => !x.DaDoc);

            // =====================================================
            // 2. LỊCH ĐANG CHỜ PT DUYỆT
            // =====================================================
            var pendingRequests =
                await _context.LichTaps
                    .Include(l => l.HoiVien)
                    .Where(l =>
                        (l.MaPT == userId.Value ||
                         l.PTUsername == ptUsername) &&
                        l.TrangThai == "ChoDuyet")
                    .OrderByDescending(l => l.Id)
                    .ToListAsync();

            ViewBag.PendingRequests =
                pendingRequests;

            ViewBag.PendingCount =
                pendingRequests.Count;

            // =====================================================
            // 3. MODEL DASHBOARD
            // =====================================================
            var model =
                new PTDashboardViewModel
                {
                    TenHLV = pt.HoTen,

                    CaLamViec =
                        "Ca Sáng (06:00 - 12:00)"
                };

            // =====================================================
            // 4. DANH SÁCH HỘI VIÊN PT QUẢN LÝ
            // =====================================================
            var hoiViens =
                await _context.LichTaps
                    .Include(x => x.HoiVien)
                    .Where(x =>
                        (x.MaPT == userId.Value ||
                         x.PTUsername == ptUsername) &&
                        x.HoiVien != null)
                    .Select(x => x.HoiVien!)
                    .Where(x =>
                        x.VaiTro == "HoiVien")
                    .GroupBy(x => x.Id)
                    .Select(g =>
                        new HoiVienBasicViewModel
                        {
                            Id = g.First().Id,

                            MaHoiVien =
                                g.First().MaHoiVien
                                ??
                                (
                                    "HV" +
                                    g.First()
                                        .Id
                                        .ToString("0000")
                                ),

                            HoTen =
                                g.First().HoTen
                        })
                    .ToListAsync();

            model.DanhSachHoiVien =
                hoiViens;

            model.TongHoiVienQuanLy =
                hoiViens.Count;

            // =====================================================
            // 5. LỊCH DẠY HÔM NAY
            // =====================================================
            var lichTapsToday =
                await _context.LichTaps
                    .Include(l => l.HoiVien)
                    .Where(l =>
                        (l.PTUsername == ptUsername ||
                         l.MaPT == userId.Value) &&
                        l.NgayTap.Date == today &&
                        l.TrangThai == "DaDuyet")
                    .OrderBy(l =>
                        l.ThoiGianBatDau)
                    .ToListAsync();

            model.SoBuoiTapHomNay =
                lichTapsToday.Count;

            model.SoBuoiDaHoanThanh =
                lichTapsToday.Count(
                    l => l.DaDiemDanh);

            model.DanhSachLichTap =
                lichTapsToday
                    .Select(l =>
                        new LichTapItemViewModel
                        {
                            Id = l.Id,

                            TenHoiVien =
                                l.HoiVien?.HoTen
                                ??
                                "Hội viên #" +
                                l.HoiVienId,

                            KhungGio =
                                $"{l.ThoiGianBatDau:HH\\:mm} - " +
                                $"{l.ThoiGianKetThuc:HH\\:mm}",

                            NoiDungTap =
                                l.NoiDung,

                            TrangThai =
                                l.DaDiemDanh
                                    ? "Đã Hoàn Thành"
                                    : "Đã Duyệt"
                        })
                    .ToList();

            // =====================================================
            // 6. CHỌN HỘI VIÊN
            // =====================================================
            int targetMemberId =
                selectedMemberId
                ??
                hoiViens
                    .FirstOrDefault()
                    ?.Id
                ??
                0;

            // =====================================================
            // 7. KIỂM TRA HỘI VIÊN THUỘC PT
            // =====================================================
            if (targetMemberId > 0)
            {
                bool memberBelongsToPt =
                    hoiViens.Any(
                        x => x.Id == targetMemberId);

                if (!memberBelongsToPt)
                {
                    targetMemberId =
                        hoiViens
                            .FirstOrDefault()
                            ?.Id
                        ??
                        0;
                }
            }

            // =====================================================
            // 8. LẤY CHỈ SỐ SỨC KHỎE HỘI VIÊN
            // =====================================================
            if (targetMemberId > 0)
            {
                var member =
                    await _context.NguoiDungs
                        .FirstOrDefaultAsync(x =>
                            x.Id == targetMemberId &&
                            x.VaiTro == "HoiVien");

                var chiSoLogs = await _context.ChiSoSucKhoes
    .Where(c => c.HoiVienId == targetMemberId)
    .OrderByDescending(c => c.NgayDo)
    .Take(5)
    .OrderBy(c => c.NgayDo)
    .ToListAsync();

                if (member != null)
                {
                    model.HoiVienChon =
                        new HoiVienChiTietViewModel
                        {
                            Id = member.Id,

                            HoTen =
                                member.HoTen,

                            ChieuCao =
                                member.ChieuCao ?? 0,

                            CanNang =
                                member.CanNang ?? 0,

                            MucTieu =
                                member.MucTieuTapLuyen
                                ??
                                "Chưa thiết lập",

                            TinhTrangSucKhoe =
                                member.GhiChuSucKhoe
                                ??
                                "Bình thường",

                            LabelsTuan =
                                chiSoLogs
                                    .Select(c =>
                                        c.NgayDo
                                            .ToString(
                                                "dd/MM"))
                                    .ToList(),

                            LichSuCanNang =
                                chiSoLogs
                                    .Select(c =>
                                        (double)c.CanNang)
                                    .ToList()
                        };
                }
            }

            return View(model);
        }

        // =========================================================
        // XEM LỊCH SỬ HỘI VIÊN (CHI SỐ SỨC KHỎE)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> MemberHistory(int memberId)
        {
            var pt = await GetCurrentPtAsync();

            if (pt == null)
                return RedirectToAction("Index", "Home");

            var member = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.Id == memberId && x.VaiTro == "HoiVien");

            if (member == null)
                return NotFound();
            var history = await _healthService.GetHistoryAsync(memberId, 50);

            var model = new DuAnCuaToi.ViewModels.PTMemberHistoryViewModel
            {
                MemberId = member.Id,
                HoTen = member.HoTen,
                MaHoiVien = member.MaHoiVien ?? ($"HV{member.Id:0000}"),
                LichSu = history
            };

            return View(model);
        }

        // =========================================================
        // PT GHI NHẬN CHỈ SỐ INBODY CHO HỌC VIÊN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMemberHealth(HealthProfileViewModel model)
        {
            var pt = await GetCurrentPtAsync();
            if (pt == null)
                return RedirectToAction("Index", "Home");

            var member = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.Id == model.HoiVienId && x.VaiTro == "HoiVien");
            if (member == null)
            {
                TempData["Error"] = "Không tìm thấy học viên.";
                return RedirectToAction(nameof(Index));
            }

            if (!model.ChieuCao.HasValue && !model.CanNang.HasValue)
            {
                TempData["Error"] = "Vui lòng nhập ít nhất chiều cao hoặc cân nặng.";
                return RedirectToAction(nameof(MemberHistory), new { memberId = model.HoiVienId });
            }

            await _healthService.SaveHealthAsync(model.HoiVienId, model);

            TempData["Success"] = $"Đã ghi nhận chỉ số InBody cho học viên {member.HoTen} thành công.";
            return RedirectToAction(nameof(MemberHistory), new { memberId = model.HoiVienId });
        }

        // =========================================================
        // AI CONTEXT FOR MEMBER (JSON) - minimal context for PT assistance
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> MemberAIContext(int memberId)
        {
            var pt = await GetCurrentPtAsync();

            if (pt == null)
                return Unauthorized();

            var aiContext = await _healthService.GetMemberAIContextAsync(memberId);

            if (aiContext == null)
                return NotFound();

            return Json(new { success = true, data = aiContext });
        }

        // =========================================================
        // PT DUYỆT LỊCH
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanLich(
            int id)
        {
            var pt =
                await GetCurrentPtAsync();

            if (pt == null)
            {
                return Unauthorized();
            }

            // Chỉ PT đang đăng nhập mới được duyệt
            // lịch thuộc PT đó.
            var lich =
                await _context.LichTaps
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        (
                            x.MaPT == pt.Id ||
                            x.PTUsername ==
                                pt.TenDangNhap
                        ));

            if (lich == null)
            {
                TempData["Error"] = "Không tìm thấy lịch tập cần duyệt.";
                return RedirectToAction(nameof(Index));
            }

            // Chặn nếu PT đã có lịch khác đã được duyệt trong cùng ngày và cùng khung giờ
            bool ptDaCoLichTrung = await _context.LichTaps.AnyAsync(x =>
                x.Id != id &&
                (x.MaPT == pt.Id || x.PTUsername == pt.TenDangNhap) &&
                x.NgayTap.Date == lich.NgayTap.Date &&
                x.KhungGio == lich.KhungGio &&
                x.TrangThai == "DaDuyet");

            if (ptDaCoLichTrung)
            {
                TempData["Error"] = $"Không thể duyệt vì bạn đã có một buổi tập khác được duyệt vào khung giờ {lich.KhungGio} ngày {lich.NgayTap:dd/MM/yyyy}!";
                return RedirectToAction(nameof(Index));
            }

            // Chặn nếu Hội viên đã có lịch khác đã được duyệt cùng khung giờ
            bool hvDaCoLichTrung = await _context.LichTaps.AnyAsync(x =>
                x.Id != id &&
                x.HoiVienId == lich.HoiVienId &&
                x.NgayTap.Date == lich.NgayTap.Date &&
                x.KhungGio == lich.KhungGio &&
                x.TrangThai == "DaDuyet");

            if (hvDaCoLichTrung)
            {
                TempData["Error"] = $"Không thể duyệt vì hội viên này đã có một lịch tập khác đã duyệt trong khung giờ {lich.KhungGio} ngày {lich.NgayTap:dd/MM/yyyy}!";
                return RedirectToAction(nameof(Index));
            }

            lich.TrangThai = "DaDuyet";

            // Đồng bộ thời gian bắt đầu và kết thúc
            if (!string.IsNullOrWhiteSpace(lich.KhungGio) && lich.KhungGio.Contains('-'))
            {
                var parts = lich.KhungGio.Split('-');
                if (parts.Length == 2)
                {
                    if (TimeSpan.TryParse(parts[0].Trim(), out TimeSpan tBatDau))
                    {
                        lich.ThoiGianBatDau = lich.NgayTap.Date.Add(tBatDau);
                    }
                    if (TimeSpan.TryParse(parts[1].Trim(), out TimeSpan tKetThuc))
                    {
                        lich.ThoiGianKetThuc = lich.NgayTap.Date.Add(tKetThuc);
                    }
                }
            }

            _context.ThongBaos.Add(
                new ThongBao
                {
                    NguoiNhanId =
                        lich.HoiVienId,

                    NoiDung =
                        $"Lịch tập ngày " +
                        $"{lich.NgayTap:dd/MM/yyyy} " +
                        $"khung giờ " +
                        $"{lich.KhungGio} " +
                        $"đã được HLV {pt.HoTen} xác nhận.",

                    DaDoc = false,

                    NgayTao =
                        DateTime.Now
                });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã duyệt lịch tập ngày {lich.NgayTap:dd/MM/yyyy} ({lich.KhungGio}) thành công!";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // PT TỪ CHỐI LỊCH
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoiLich(
            int id)
        {
            var pt =
                await GetCurrentPtAsync();

            if (pt == null)
            {
                return Unauthorized();
            }

            // Chỉ PT đang đăng nhập mới được từ chối
            // lịch thuộc PT đó.
            var lich =
                await _context.LichTaps
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        (
                            x.MaPT == pt.Id ||
                            x.PTUsername ==
                                pt.TenDangNhap
                        ));

            if (lich == null)
            {
                TempData["Error"] = "Không tìm thấy lịch tập cần từ chối.";
                return RedirectToAction(nameof(Index));
            }

            lich.TrangThai = "TuChoi";

            _context.ThongBaos.Add(
                new ThongBao
                {
                    NguoiNhanId =
                        lich.HoiVienId,

                    NoiDung =
                        $"Lịch tập ngày " +
                        $"{lich.NgayTap:dd/MM/yyyy} " +
                        $"khung giờ " +
                        $"{lich.KhungGio} " +
                        $"đã bị HLV {pt.HoTen} từ chối.",

                    DaDoc = false,

                    NgayTao =
                        DateTime.Now
                });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã từ chối lịch tập ngày {lich.NgayTap:dd/MM/yyyy} ({lich.KhungGio}).";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // PT ĐIỂM DANH / HOÀN THÀNH BUỔI TẬP
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiemDanhBuoiTap(int id)
        {
            var pt = await GetCurrentPtAsync();

            if (pt == null)
            {
                return Unauthorized();
            }

            var lich = await _context.LichTaps
                .Include(x => x.HoiVien)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    (
                        x.MaPT == pt.Id ||
                        x.PTUsername == pt.TenDangNhap
                    ));

            if (lich == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin lịch tập.";
                return RedirectToAction(nameof(Index));
            }

            if (lich.TrangThai != "DaDuyet")
            {
                TempData["Error"] = "Chỉ có thể điểm danh các buổi tập đã được xác nhận duyệt.";
                return RedirectToAction(nameof(Index));
            }

            if (lich.DaDiemDanh)
            {
                TempData["Success"] = "Buổi tập này đã được điểm danh hoàn thành trước đó.";
                return RedirectToAction(nameof(Index));
            }

            lich.DaDiemDanh = true;

            // Thông báo cho Hội viên
            _context.ThongBaos.Add(
                new ThongBao
                {
                    NguoiNhanId = lich.HoiVienId,
                    NoiDung = $"Buổi tập ngày {lich.NgayTap:dd/MM/yyyy} (khung giờ {lich.KhungGio}) đã được HLV {pt.HoTen} điểm danh hoàn thành.",
                    DaDoc = false,
                    NgayTao = DateTime.Now
                });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã điểm danh hoàn thành buổi tập của hội viên {lich.HoiVien?.HoTen ?? "Hội viên"}!";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT PROFILE - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var id =
                GetCurrentPtId();

            if (id == null)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var user =
                await _context.NguoiDungs
                    .FirstOrDefaultAsync(x =>
                        x.Id == id.Value &&
                        x.VaiTro == "PT");

            if (user == null)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var model =
                new EditProfileViewModel
                {
                    Id = user.Id,

                    HoTen =
                        user.HoTen,

                    TenDangNhap =
                        user.TenDangNhap
                };

            return View(model);
        }

        // =========================================================
        // EDIT PROFILE - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
            EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var sessionId =
                GetCurrentPtId();

            if (sessionId == null)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            // Không cho PT sửa tài khoản khác.
            if (sessionId.Value != model.Id)
            {
                return Forbid();
            }

            var user =
                await _context.NguoiDungs
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.Id &&
                        x.VaiTro == "PT");

            if (user == null)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            model.HoTen = model.HoTen.Trim();
            model.TenDangNhap = model.TenDangNhap.Trim();

            var usernameExists = await _context.NguoiDungs
                .AnyAsync(x => x.Id != user.Id && x.TenDangNhap == model.TenDangNhap);

            if (usernameExists)
            {
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã được sử dụng.");
                return View(model);
            }

            user.HoTen =
                model.HoTen;

            user.TenDangNhap =
                model.TenDangNhap;

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
                    isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(model.MatKhauHienTai, user.MatKhauHash);
                }
                catch
                {
                    isOldPasswordCorrect = (model.MatKhauHienTai == user.MatKhauHash);
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

                user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.MatKhauMoi);
            }

            // Đồng bộ username PT
            // vào các lịch đã phân công.
            var lichs =
                await _context.LichTaps
                    .Where(x =>
                        x.MaPT == user.Id)
                    .ToListAsync();

            foreach (var item in lichs)
            {
                item.PTUsername =
                    user.TenDangNhap;
            }

            await _context.SaveChangesAsync();

            // Cập nhật Session.
            HttpContext.Session.SetString(
                "Username",
                user.TenDangNhap ?? "");

            HttpContext.Session.SetString(
                "UserName",
                user.HoTen ?? "");

            TempData["Success"] =
                "Đã cập nhật thông tin cá nhân.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // API - LẤY CHI TIẾT HỘI VIÊN
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetMemberDetails(
            int id)
        {
            var userId =
                GetCurrentPtId();

            if (userId == null)
            {
                return Unauthorized();
            }

            // =====================================================
            // KIỂM TRA PT
            // =====================================================
            var pt =
                await _context.NguoiDungs
                    .FirstOrDefaultAsync(x =>
                        x.Id == userId.Value &&
                        x.VaiTro == "PT");

            if (pt == null)
            {
                return Forbid();
            }

            // =====================================================
            // CHỈ ĐƯỢC XEM HỘI VIÊN THUỘC PT
            // =====================================================
            bool memberBelongsToPt =
                await _context.LichTaps
                    .AnyAsync(x =>
                        (
                            x.MaPT == pt.Id ||
                            x.PTUsername ==
                                pt.TenDangNhap
                        ) &&
                        x.HoiVienId == id);

            if (!memberBelongsToPt)
            {
                return Forbid();
            }

            // =====================================================
            // LẤY HỘI VIÊN
            // =====================================================
            var member =
                await _context.NguoiDungs
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.VaiTro == "HoiVien");

            if (member == null)
            {
                return NotFound();
            }

            // =====================================================
            // LẤY LỊCH SỬ CHỈ SỐ
            // =====================================================
            var chiSoLogs =
                await _context.ChiSoSucKhoes
                    .Where(c =>
                        c.HoiVienId == id)
                    .OrderBy(c => c.NgayDo)
                    .Take(5)
                    .ToListAsync();

            // =====================================================
            // TÍNH BMI
            // =====================================================
            double bmi = 0;

            if (member.ChieuCao.HasValue &&
                member.CanNang.HasValue &&
                member.ChieuCao.Value > 0)
            {
                bmi =
                    member.CanNang.Value /
                    Math.Pow(
                        member.ChieuCao.Value / 100.0,
                        2);
            }

            // =====================================================
            // TRẢ JSON CHO FRONTEND
            // =====================================================
            return Json(
                new
                {
                    hoTen =
                        member.HoTen,

                    maHoiVien =
                        member.MaHoiVien
                        ??
                        (
                            "HV" +
                            member.Id
                                .ToString("0000")
                        ),

                    chieuCao =
                        member.ChieuCao,

                    canNang =
                        member.CanNang,

                    bmi =
                        Math.Round(
                            bmi,
                            1),

                    mucTieu =
                        member.MucTieuTapLuyen
                        ??
                        "Chưa thiết lập",

                    sucKhoe =
                        member.GhiChuSucKhoe
                        ??
                        "Bình thường",

                    labels =
                        chiSoLogs
                            .Select(c =>
                                c.NgayDo
                                    .ToString(
                                        "dd/MM"))
                            .ToList(),

                    weights =
                        chiSoLogs
                            .Select(c =>
                                c.CanNang)
                            .ToList()
                });
        }
    }
}

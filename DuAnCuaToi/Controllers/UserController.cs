using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using DuAnCuaToi.Services;
using DuAnCuaToi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QRCoder;
using static DuAnCuaToi.Models.AIChatRequest;

namespace DuAnCuaToi.Controllers
{
    public class UserController : Controller
    {
        // =========================================================
        // LẤY ID HỘI VIÊN ĐANG ĐĂNG NHẬP
        // =========================================================
        private int? GetCurrentMemberId()
        {
            var userIdStr =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr))
                return null;

            if (!int.TryParse(userIdStr, out int userId))
                return null;

            return userId;
        }
        private readonly ApplicationDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly IHealthService _healthService;
        private readonly IAIContextBuilder _aiContextBuilder;
        private readonly IMemoryCache _cache;

        public UserController(
            ApplicationDbContext context,
            IGeminiService geminiService,
            IHealthService healthService,
            IAIContextBuilder aiContextBuilder,
            IMemoryCache cache)
        {
            _context = context;
            _geminiService = geminiService;
            _healthService = healthService;
            _aiContextBuilder = aiContextBuilder;
            _cache = cache;
        }

        // =========================================================
        // KIỂM TRA GIỚI HẠN TẦN SUẤT GỌI AI (RATE LIMITING CHỐNG SPAM)
        // =========================================================
        private bool CheckRateLimit(string actionKey, int userId, int maxRequests, TimeSpan window)
        {
            var cacheKey = $"RateLimit_{actionKey}_{userId}";
            var currentCount = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return 0;
            });

            if (currentCount >= maxRequests)
            {
                return false;
            }

            _cache.Set(cacheKey, currentCount + 1, window);
            return true;
        }

        // =====================================================
        // SYNC: cập nhật NguoiDung từ ChiSoSucKhoe mới nhất
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> SyncMember(int id)
        {
            var role = HttpContext.Session.GetString("Role") ?? "";

            if (role != "Admin" && role != "AdminGym" && role != "PT")
                return Unauthorized();

            await _healthService.SyncMemberFromLatestAsync(id);

            return Json(new { success = true });
        }

        // =====================================================
        // SYNC ALL: cập nhật toàn bộ hội viên từ bản ghi mới nhất
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> SyncAllMembers()
        {
            var role = HttpContext.Session.GetString("Role") ?? "";

            if (role != "Admin" && role != "AdminGym")
                return Unauthorized();

            var count = await _healthService.SyncAllMembersFromLatestAsync();

            return Json(new { success = true, count });
        }

        // =========================================================
        // TRANG CHỦ HỘI VIÊN
        // =========================================================
        // ======================
        // Trang chủ Hội viên
        // ======================
        public IActionResult Index()
        {
            // ========================================
            // 1. LẤY ID HỘI VIÊN ĐANG ĐĂNG NHẬP
            // ========================================
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            // ========================================
            // 2. LẤY THÔNG TIN HỘI VIÊN
            // ========================================
            var currentUser = _context.NguoiDungs
                .FirstOrDefault(u => u.Id == userId);

            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.CurrentUser = currentUser;

            // ========================================
            // 3. GÓI TẬP ĐANG HOẠT ĐỘNG
            // ========================================
            var goiTapDangHoatDong = _context.DangKyGoiTaps
                .Include(x => x.GoiTap)
                .Where(x =>
                    x.NguoiDungId == userId &&
                    x.TrangThai &&
                    x.NgayHetHan >= DateTime.Now)
                .OrderByDescending(x => x.NgayHetHan)
                .FirstOrDefault();

            ViewBag.GoiTapDangHoatDong = goiTapDangHoatDong;

            // ========================================
            // 4. THÔNG BÁO MỚI NHẤT
            // ========================================
            var thongBaoMoiNhat = _context.ThongBaos
                .Where(x => x.NguoiNhanId == userId)
                .OrderByDescending(x => x.NgayTao)
                .FirstOrDefault();

            ViewBag.ThongBaoMoiNhat = thongBaoMoiNhat;

            // ========================================
            // 5. SỐ THÔNG BÁO CHƯA ĐỌC
            // ========================================
            var soThongBaoChuaDoc = _context.ThongBaos
                .Count(x =>
                    x.NguoiNhanId == userId &&
                    !x.DaDoc);

            ViewBag.SoThongBaoChuaDoc = soThongBaoChuaDoc;

            // ========================================
            // 6. DANH SÁCH PT
            // ========================================
            ViewBag.DanhSachPT = _context.NguoiDungs
                .Where(u => u.VaiTro == "PT")
                .ToList();

            // ========================================
            // 7. KHUNG GIỜ TẬP
            // ========================================
            ViewBag.KhungGioList = new List<string>
    {
        "06:00 - 08:00",
        "08:00 - 10:00",
        "10:00 - 12:00",
        "14:00 - 16:00",
        "16:00 - 18:00",
        "18:00 - 20:00"
    };

            return View();
        }


        // =========================================================
        // TRANG THÔNG BÁO
        // =========================================================
        public IActionResult ThongBao()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var thongBaos = _context.ThongBaos
                .Where(x => x.NguoiNhanId == userId)
                .OrderByDescending(x => x.NgayTao)
                .ToList();

            return View(thongBaos);
        }


        // =========================================================
        // GÓI TẬP CỦA TÔI + DANH SÁCH GÓI ĐANG BÁN
        // =========================================================
        [HttpGet]
        public IActionResult GoiTapCuaToi()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = _context.NguoiDungs
                .FirstOrDefault(x =>
                    x.Id == userId &&
                    x.VaiTro == "HoiVien");

            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // =====================================================
            // 1. LỊCH SỬ / GÓI ĐÃ ĐĂNG KÝ
            // =====================================================

            var goiTapsCuaToi = _context.DangKyGoiTaps
                .Include(x => x.GoiTap)
                    .ThenInclude(x => x!.GoiTapDichVus)
                        .ThenInclude(x => x.DichVu)
                .Where(x => x.NguoiDungId == userId)
                .OrderByDescending(x => x.NgayDangKy)
                .ToList();

            // =====================================================
            // 2. CÁC GÓI ĐANG ĐƯỢC MỞ BÁN
            // =====================================================

            var danhSachGoi = _context.GoiTaps
                .Include(x => x.GoiTapDichVus)
                    .ThenInclude(x => x.DichVu)
                .OrderBy(x => x.Gia)
                .ToList();

            // =====================================================
            // 3. HÓA ĐƠN CHƯA THANH TOÁN / CHỜ XÁC NHẬN
            // =====================================================

            var hoaDonChoThanhToan = _context.HoaDons
                .Include(x => x.DangKyGoiTap)
                    .ThenInclude(x => x!.GoiTap)
                .Where(x =>
                    x.DangKyGoiTap != null &&
                    x.DangKyGoiTap.NguoiDungId == userId &&
                    (
                        x.TrangThai == "Chưa thanh toán" ||
                        x.TrangThai == "Chờ xác nhận"
                    ))
                .OrderByDescending(x => x.NgayLap)
                .ToList();

            ViewBag.DanhSachGoi = danhSachGoi;

            ViewBag.HoaDonChoThanhToan =
                hoaDonChoThanhToan;

            ViewBag.CurrentUser = currentUser;

            return View(goiTapsCuaToi);
        }
        // =========================================================
        // ĐĂNG KÝ GÓI TẬP
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKyGoiTap(int goiTapId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var hoiVien = _context.NguoiDungs
                .FirstOrDefault(x =>
                    x.Id == userId &&
                    x.VaiTro == "HoiVien");

            if (hoiVien == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // =====================================================
            // LẤY GÓI
            // =====================================================

            var goiTap = _context.GoiTaps
                .Include(x => x.GoiTapDichVus)
                    .ThenInclude(x => x.DichVu)
                .FirstOrDefault(x => x.Id == goiTapId);

            if (goiTap == null)
            {
                TempData["Error"] =
                    "Không tìm thấy gói tập.";

                return RedirectToAction(nameof(GoiTapCuaToi));
            }

            // =====================================================
            // KIỂM TRA ĐĂNG KÝ ĐANG CHỜ
            // =====================================================

            var dangKyCho = _context.DangKyGoiTaps
                .Include(x => x.HoaDons)
                .FirstOrDefault(x =>
                    x.NguoiDungId == userId &&
                    x.GoiTapId == goiTapId &&
                    !x.TrangThai &&
                    x.HoaDons.Any(h =>
                        h.TrangThai == "Chưa thanh toán" ||
                        h.TrangThai == "Chờ xác nhận"));

            if (dangKyCho != null)
            {
                TempData["Error"] =
                    "Bạn đã có một đăng ký gói này đang chờ thanh toán.";

                return RedirectToAction(nameof(GoiTapCuaToi));
            }

            // =====================================================
            // TẠO ĐĂNG KÝ
            // =====================================================

            var ngayDangKy = DateTime.Now;

            var dangKy = new DangKyGoiTap
            {
                NguoiDungId = userId,
                GoiTapId = goiTap.Id,

                // Tạm đặt thời gian.
                // Khi lễ tân xác nhận thanh toán,
                // hệ thống sẽ tính lại từ thời điểm thanh toán.
                NgayDangKy = ngayDangKy,

                NgayHetHan =
                    ngayDangKy.AddDays(goiTap.ThoiHanNgay),

                TrangThai = false
            };

            _context.DangKyGoiTaps.Add(dangKy);

            _context.SaveChanges();

            // =====================================================
            // TẠO HÓA ĐƠN
            // =====================================================

            var hoaDon = new HoaDon
            {
                DangKyGoiTapId = dangKy.Id,
                TongTien = goiTap.Gia,
                NgayLap = DateTime.Now,
                TrangThai = "Chưa thanh toán"
            };

            _context.HoaDons.Add(hoaDon);

            // =====================================================
            // THÔNG BÁO
            // =====================================================

            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = userId,

                NoiDung =
                    $"Bạn đã đăng ký gói {goiTap.TenGoi}. " +
                    $"Hóa đơn #{hoaDon.Id} đang chờ thanh toán chuyển khoản.",

                DaDoc = false,
                NgayTao = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Success"] =
                $"Đã tạo đăng ký gói {goiTap.TenGoi}. " +
                $"Vui lòng thực hiện thanh toán chuyển khoản.";

            return RedirectToAction(
                nameof(ThanhToanGoiTap),
                new { id = hoaDon.Id });
        }
        // =========================================================
        // TRANG THANH TOÁN GÓI TẬP - CHỈ CHUYỂN KHOẢN
        // =========================================================
        [HttpGet]
        public IActionResult ThanhToanGoiTap(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var hoaDon = _context.HoaDons
                .Include(x => x.DangKyGoiTap)
                    .ThenInclude(x => x!.NguoiDung)
                .Include(x => x.DangKyGoiTap)
                    .ThenInclude(x => x!.GoiTap)
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.DangKyGoiTap != null &&
                    x.DangKyGoiTap.NguoiDungId == userId);

            if (hoaDon == null)
            {
                TempData["Error"] =
                    "Không tìm thấy hóa đơn.";

                return RedirectToAction(
                    nameof(GoiTapCuaToi));
            }

            // =====================================================
            // TẠO QR DEMO CHO HÓA ĐƠN
            // =====================================================

            string noiDungQr =
                $"SMARTGYM-DEMO|" +
                $"HOADON:{hoaDon.Id}|" +
                $"HOIVIEN:{hoaDon.DangKyGoiTap?.NguoiDung?.MaHoiVien}|" +
                $"GOI:{hoaDon.DangKyGoiTap?.GoiTap?.TenGoi}|" +
                $"SOTIEN:{hoaDon.TongTien:0}";

            using QRCodeGenerator qrGenerator = new();
            using QRCodeData qrData = qrGenerator.CreateQrCode(
                noiDungQr,
                QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qrCode = new(qrData);

            ViewBag.QRCode = Convert.ToBase64String(
                qrCode.GetGraphic(20));

            ViewBag.QRContent = noiDungQr;

            return View(hoaDon);
        }

        // =========================================================
        // HỦY ĐĂNG KÝ ĐANG CHỜ THANH TOÁN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyDangKyChoThanhToan(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var hoaDon = _context.HoaDons
                .Include(x => x.ThanhToans)
                .Include(x => x.DangKyGoiTap)
                    .ThenInclude(x => x!.GoiTap)
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.DangKyGoiTap != null &&
                    x.DangKyGoiTap.NguoiDungId == userId);

            if (hoaDon == null ||
                hoaDon.DangKyGoiTap == null)
            {
                TempData["Error"] =
                    "Không tìm thấy đăng ký cần hủy.";

                return RedirectToAction(
                    nameof(GoiTapCuaToi));
            }

            if (hoaDon.TrangThai != "Chưa thanh toán")
            {
                TempData["Error"] =
                    "Chỉ có thể hủy đơn đang ở trạng thái Chưa thanh toán.";

                return RedirectToAction(
                    nameof(GoiTapCuaToi));
            }

            if (hoaDon.ThanhToans.Any())
            {
                TempData["Error"] =
                    "Đơn này đã phát sinh giao dịch nên không thể hủy.";

                return RedirectToAction(
                    nameof(GoiTapCuaToi));
            }

            var dangKy = hoaDon.DangKyGoiTap;
            var tenGoi = dangKy.GoiTap?.TenGoi ?? "gói tập";

            // Xóa hóa đơn trước, sau đó xóa đăng ký gói đang chờ.
            _context.HoaDons.Remove(hoaDon);
            _context.DangKyGoiTaps.Remove(dangKy);

            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = userId,
                NoiDung = $"Bạn đã hủy đăng ký {tenGoi} đang chờ thanh toán.",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Success"] =
                $"Đã hủy đăng ký {tenGoi}.";

            return RedirectToAction(
                nameof(GoiTapCuaToi));
        }

        // =========================================================
        // HỘI VIÊN XÁC NHẬN ĐÃ CHUYỂN KHOẢN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanDaChuyenKhoan(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var hoaDon = _context.HoaDons
                .Include(x => x.DangKyGoiTap)
                    .ThenInclude(x => x!.GoiTap)
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.DangKyGoiTap != null &&
                    x.DangKyGoiTap.NguoiDungId == userId);

            if (hoaDon == null)
            {
                TempData["Error"] =
                    "Không tìm thấy hóa đơn.";

                return RedirectToAction(
                    nameof(GoiTapCuaToi));
            }

            if (hoaDon.TrangThai == "Đã thanh toán")
            {
                TempData["Error"] =
                    "Hóa đơn này đã được thanh toán.";

                return RedirectToAction(
                    nameof(GoiTapCuaToi));
            }

            if (hoaDon.TrangThai == "Chờ xác nhận")
            {
                TempData["Error"] =
                    "Bạn đã gửi xác nhận cho hóa đơn này.";

                return RedirectToAction(
                    nameof(GoiTapCuaToi));
            }

            // =====================================================
            // KHÔNG TẠO ThanhToan Ở ĐÂY
            //
            // Vì hội viên chỉ thông báo:
            // "Tôi đã chuyển khoản".
            //
            // Lễ tân mới là người xác nhận giao dịch thật.
            // =====================================================

            hoaDon.TrangThai = "Chờ xác nhận";

            var goi = hoaDon.DangKyGoiTap?.GoiTap;

            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = userId,

                NoiDung =
                    $"Bạn đã gửi xác nhận chuyển khoản " +
                    $"cho hóa đơn #{hoaDon.Id}" +
                    $"{(goi != null ? $" - gói {goi.TenGoi}" : "")}. " +
                    $"Vui lòng chờ lễ tân xác nhận giao dịch.",

                DaDoc = false,
                NgayTao = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Success"] =
                "Đã gửi xác nhận chuyển khoản. " +
                "Vui lòng chờ lễ tân kiểm tra giao dịch.";

            return RedirectToAction(
                nameof(GoiTapCuaToi));
        }

        // =========================================================
        // XÁC NHẬN ĐIỂM DANH
        // =========================================================
        [HttpPost]
        public IActionResult ConfirmCheckIn()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                TempData["Error"] =
                    "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại.";

                return RedirectToAction("Index", "Home");
            }

            var currentUser = _context.NguoiDungs
                .FirstOrDefault(u => u.Id == userId);

            if (currentUser == null)
            {
                TempData["Error"] =
                    "Không tìm thấy thông tin hội viên.";

                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra gói tập
            var goiTapDangSuDung = _context.DangKyGoiTaps
                .FirstOrDefault(x =>
                    x.NguoiDungId == userId &&
                    x.TrangThai &&
                    x.NgayHetHan >= DateTime.Now);

            if (goiTapDangSuDung == null)
            {
                TempData["Error"] =
                    "Gói tập của bạn không còn hiệu lực.";

                return RedirectToAction("Index");
            }

            // Kiểm tra đã điểm danh hôm nay
            var batDauNgay = DateTime.Today;
            var ketThucNgay = batDauNgay.AddDays(1);

            var daCheckInHomNay = _context.CheckIns
                .Any(x =>
                    x.NguoiDungId == userId &&
                    x.ThoiGian >= batDauNgay &&
                    x.ThoiGian < ketThucNgay);

            if (daCheckInHomNay)
            {
                TempData["Error"] =
                    "Bạn đã điểm danh hôm nay rồi.";

                return RedirectToAction("Index");
            }

            // =====================================================
            // TẠO BẢN GHI ĐIỂM DANH
            // =====================================================
            var checkIn = new CheckIn
            {
                NguoiDungId = userId,
                ThoiGian = DateTime.Now,
                HinhThuc = "QR"
            };

            _context.CheckIns.Add(checkIn);

            // =====================================================
            // TẠO THÔNG BÁO CHO HỘI VIÊN
            // =====================================================
            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = userId,
                NoiDung =
                    $"Bạn đã điểm danh thành công lúc {DateTime.Now:HH:mm - dd/MM/yyyy}.",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Success"] =
                "Điểm danh thành công! Chúc bạn có buổi tập hiệu quả.";

            return RedirectToAction("Index");
        }


        // ======================
        // Sửa thông tin cá nhân
        // ======================
        [HttpGet]
        public IActionResult EditProfile()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = _context.NguoiDungs
                .FirstOrDefault(x => x.Id == userId && x.VaiTro == "HoiVien");

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new EditProfileViewModel
            {
                Id = user.Id,
                HoTen = user.HoTen,
                TenDangNhap = user.TenDangNhap
            };

            return View(model);
        }


        // =========================================================
        // THÔNG TIN CÁ NHÂN - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var sessionId = GetCurrentMemberId();
            if (sessionId == null || sessionId.Value != model.Id)
                return Forbid();

            var user = _context.NguoiDungs
                .FirstOrDefault(x => x.Id == model.Id && x.VaiTro == "HoiVien");

            if (user == null)
                return RedirectToAction("Index", "Home");

            model.HoTen = model.HoTen.Trim();
            model.TenDangNhap = model.TenDangNhap.Trim();

            if (_context.NguoiDungs.Any(x => x.Id != user.Id && x.TenDangNhap == model.TenDangNhap))
            {
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã được sử dụng.");
                return View(model);
            }

            user.HoTen = model.HoTen;
            user.TenDangNhap = model.TenDangNhap;

            if (!string.IsNullOrWhiteSpace(model.MatKhauMoi))
            {
                user.MatKhauHash =
                    BCrypt.Net.BCrypt.HashPassword(model.MatKhauMoi);
            }

            _context.SaveChanges();

            HttpContext.Session.SetString(
                "Username",
                user.TenDangNhap);

            HttpContext.Session.SetString(
                "UserName",
                user.HoTen);

            TempData["Success"] =
                "Cập nhật thông tin thành công.";

            return RedirectToAction("Index");
        }


        // =========================================================
        // ĐẶT LỊCH TẬP
        // =========================================================
        [HttpPost]
        public IActionResult DatLich(
            int maPt,
            DateTime ngayTap,
            string khungGio,
            string? ghiChu)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var hoiVienHienTai = _context.NguoiDungs
                .FirstOrDefault(x => x.Id == userId);

            if (hoiVienHienTai == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var pt = _context.NguoiDungs
                .FirstOrDefault(x =>
                    x.Id == maPt &&
                    x.VaiTro == "PT");

            if (pt == null)
            {
                TempData["Error"] =
                    "Không tìm thấy PT.";

                return RedirectToAction("Index");
            }

            var lichTapMoi = new LichTap
            {
                MaHoiVien = hoiVienHienTai.Id,
                MaPT = maPt,
                PTUsername = pt.TenDangNhap,
                NgayTap = ngayTap,
                KhungGio = khungGio,
                GhiChu = ghiChu,
                TrangThai = "ChoDuyet",
                NgayTao = DateTime.Now
            };

            _context.LichTaps.Add(lichTapMoi);

            // Thông báo cho PT
            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = maPt,
                NoiDung =
                    $"{hoiVienHienTai.HoTen} vừa gửi yêu cầu đặt lịch.",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Success"] =
                "Yêu cầu đăng ký lịch tập đã được gửi!";

            return RedirectToAction("Index");
        }
        // =========================================================
        // LỊCH TẬP CỦA TÔI
        // =========================================================
        [HttpGet]
        public IActionResult LichTapCuaToi()
        {
            // ---------------------------------------------------------
            // 1. LẤY HỘI VIÊN ĐANG ĐĂNG NHẬP
            // ---------------------------------------------------------
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            // ---------------------------------------------------------
            // 2. KIỂM TRA TÀI KHOẢN
            // ---------------------------------------------------------
            var currentUser = _context.NguoiDungs
                .FirstOrDefault(x =>
                    x.Id == userId &&
                    x.VaiTro == "HoiVien");

            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // ---------------------------------------------------------
            // 3. LẤY TOÀN BỘ LỊCH TẬP CỦA HỘI VIÊN
            // ---------------------------------------------------------
            var lichTaps = _context.LichTaps
                .Include(x => x.HoiVien)
                .Include(x => x.PT)
                .Where(x => x.HoiVienId == userId)
                .OrderByDescending(x => x.NgayTap)
                .ThenByDescending(x => x.ThoiGianBatDau)
                .ToList();

            // ---------------------------------------------------------
            // 4. TRUYỀN THÔNG TIN HỘI VIÊN CHO VIEW
            // ---------------------------------------------------------
            ViewBag.CurrentUser = currentUser;

            // ---------------------------------------------------------
            // 5. THỐNG KÊ
            // ---------------------------------------------------------
            ViewBag.TongLich = lichTaps.Count;

            ViewBag.SoLichChoDuyet =
                lichTaps.Count(x =>
                    x.TrangThai == "ChoDuyet");

            ViewBag.SoLichDaDuyet =
                lichTaps.Count(x =>
                    x.TrangThai == "DaDuyet");

            ViewBag.SoLichTuChoi =
                lichTaps.Count(x =>
                    x.TrangThai == "TuChoi");

            return View(lichTaps);
        }
        // =========================================================
        // LỊCH SỬ ĐIỂM DANH
        // =========================================================
        [HttpGet]
        public IActionResult LichSuDiemDanh()
        {
            // ---------------------------------------------------------
            // 1. LẤY HỘI VIÊN ĐANG ĐĂNG NHẬP
            // ---------------------------------------------------------
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            // ---------------------------------------------------------
            // 2. KIỂM TRA TÀI KHOẢN
            // ---------------------------------------------------------
            var currentUser = _context.NguoiDungs
                .FirstOrDefault(x =>
                    x.Id == userId &&
                    x.VaiTro == "HoiVien");

            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // ---------------------------------------------------------
            // 3. LẤY LỊCH SỬ CHECK-IN
            // ---------------------------------------------------------
            var checkIns = _context.CheckIns
                .Where(x => x.NguoiDungId == userId)
                .OrderByDescending(x => x.ThoiGian)
                .ToList();

            // ---------------------------------------------------------
            // 4. THÔNG TIN HỘI VIÊN
            // ---------------------------------------------------------
            ViewBag.CurrentUser = currentUser;

            // ---------------------------------------------------------
            // 5. THỐNG KÊ
            // ---------------------------------------------------------
            ViewBag.TongLuotDiemDanh = checkIns.Count;

            ViewBag.DiemDanhThangNay =
                checkIns.Count(x =>
                    x.ThoiGian.Month == DateTime.Now.Month &&
                    x.ThoiGian.Year == DateTime.Now.Year);

            return View(checkIns);
        }
        // =========================================================
        // LỊCH SỬ THANH TOÁN
        // =========================================================
        [HttpGet]
        public IActionResult LichSuThanhToan()
        {
            // ---------------------------------------------------------
            // 1. LẤY HỘI VIÊN ĐANG ĐĂNG NHẬP
            // ---------------------------------------------------------
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) ||
                !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            // ---------------------------------------------------------
            // 2. KIỂM TRA TÀI KHOẢN
            // ---------------------------------------------------------
            var currentUser = _context.NguoiDungs
                .FirstOrDefault(x =>
                    x.Id == userId &&
                    x.VaiTro == "HoiVien");

            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // ---------------------------------------------------------
            // 3. LẤY HÓA ĐƠN CỦA HỘI VIÊN
            //
            // HoaDon
            //   ↓
            // DangKyGoiTap
            //   ↓
            // GoiTap
            //
            // Đồng thời lấy ThanhToans.
            // ---------------------------------------------------------
            var hoaDons = _context.HoaDons
                .Include(x => x.DangKyGoiTap)
                    .ThenInclude(x => x.GoiTap)
                .Include(x => x.ThanhToans)
                .Where(x =>
                    x.DangKyGoiTap != null &&
                    x.DangKyGoiTap.NguoiDungId == userId)
                .OrderByDescending(x => x.NgayLap)
                .ToList();

            // ---------------------------------------------------------
            // 4. THÔNG TIN HỘI VIÊN
            // ---------------------------------------------------------
            ViewBag.CurrentUser = currentUser;

            // ---------------------------------------------------------
            // 5. THỐNG KÊ
            // ---------------------------------------------------------
            ViewBag.TongHoaDon = hoaDons.Count;

            ViewBag.SoHoaDonDaThanhToan =
                hoaDons.Count(x =>
                    string.Equals(
                        x.TrangThai,
                        "Đã thanh toán",
                        StringComparison.OrdinalIgnoreCase));

            // ---------------------------------------------------------
            // 6. TỔNG TIỀN ĐÃ THANH TOÁN
            // ---------------------------------------------------------
            ViewBag.TongTienDaThanhToan =
                hoaDons
                    .Where(x =>
                        string.Equals(
                            x.TrangThai,
                            "Đã thanh toán",
                            StringComparison.OrdinalIgnoreCase))
                    .SelectMany(x => x.ThanhToans)
                    .Where(x =>
                        string.Equals(
                            x.TrangThai,
                            "Đã thanh toán",
                            StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.SoTien);

            return View(hoaDons);
        }
        // =========================================================
        // TRANG TRỢ LÝ AI
        // =========================================================
        [HttpGet]
        public IActionResult TroLyAI()
        {
            var userId = GetCurrentMemberId();

            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var member = _context.NguoiDungs
                .FirstOrDefault(x =>
                    x.Id == userId.Value &&
                    x.VaiTro == "HoiVien");

            if (member == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }


        // =========================================================
        // FR8 - AI TÓM TẮT INBODY
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> AiTomTatInBody()
        {
            var userId = GetCurrentMemberId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại."
                });
            }

            if (!CheckRateLimit("AiTomTatInBody", userId.Value, maxRequests: 10, window: TimeSpan.FromDays(1)))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Bạn đã sử dụng hết 10 lượt tóm tắt InBody trong ngày hôm nay. Hãy duy trì tập luyện và quay lại vào ngày mai nhé! 📊"
                });
            }

            try
            {
                // =====================================================
                // CHỈ LẤY DỮ LIỆU CỦA HỘI VIÊN ĐANG ĐĂNG NHẬP
                // =====================================================
                var memberContext =
                    _aiContextBuilder.BuildMemberAIContext(userId.Value);

                // FR8 không cần thông tin định danh hay dữ liệu thanh toán.
                // Giảm PII/token trước khi gửi sang Gemini.
                memberContext.HoTen = "";
                memberContext.MaHoiVien = "";
                memberContext.ThanhToan.Clear();

                var contextJson =
                    Newtonsoft.Json.JsonConvert.SerializeObject(
                        memberContext,
                        Newtonsoft.Json.Formatting.Indented);

                var result =
                    await _geminiService.GenerateProgressSummaryAsync(
                        contextJson);

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (GeminiServiceException ex)
            {
                return StatusCode(ex.HttpStatusCode, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Không thể phân tích chỉ số lúc này. Vui lòng thử lại."
                });
            }
        }


        // =========================================================
        // FR9 - AI SINH LỊCH TẬP 7 NGÀY
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> AiSinhLich(
    [FromBody] AIWorkoutRequest request)
        {
            var userId = GetCurrentMemberId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại."
                });
            }

            if (!CheckRateLimit("AiSinhLich", userId.Value, maxRequests: 5, window: TimeSpan.FromDays(1)))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Bạn đã đạt giới hạn 5 lần sinh lịch tập trong ngày. Hãy kiên trì thực hiện lịch tập hiện tại nhé! 🎯"
                });
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu yêu cầu không hợp lệ."
                });
            }

            var goal =
                request.Goal?.Trim() ?? "";

            var availability =
                request.Availability?.Trim() ?? "";

            var level =
                request.Level?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(goal))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng chọn mục tiêu tập luyện."
                });
            }

            if (string.IsNullOrWhiteSpace(availability))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng nhập thời gian rảnh."
                });
            }

            if (string.IsNullOrWhiteSpace(level))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng chọn trình độ."
                });
            }

            try
            {
                // Context riêng cho FR9
                var workoutContext =
                    _aiContextBuilder.BuildWorkoutAIContext(userId.Value);

                var result =
                    await _geminiService.GenerateWorkoutPlanAsync(
                        goal,
                        availability,
                        level,
                        workoutContext);

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (GeminiServiceException ex)
            {
                return StatusCode(ex.HttpStatusCode, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "AI hiện không thể tạo lịch tập. Vui lòng thử lại sau."
                });
            }
        }

        // =========================================================
        // FR10 - AI CHAT HỘI VIÊN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> AiChat(
            [FromBody] AIChatRequest request)
        {
            var userId =
                GetCurrentMemberId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại."
                });
            }


            if (request == null ||
                string.IsNullOrWhiteSpace(
                    request.Question))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Vui lòng nhập câu hỏi."
                });
            }


            var question =
                request.Question.Trim();


            // =====================================================
            // GIỚI HẠN INPUT
            // =====================================================
            if (question.Length > 600)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Câu hỏi không được vượt quá 600 ký tự."
                });
            }

            // =====================================================
            // GIỚI HẠN TẦN SUẤT (RATE LIMITING: 20 LƯỢT / GIỜ)
            // =====================================================
            if (!CheckRateLimit("AiChat", userId.Value, maxRequests: 20, window: TimeSpan.FromHours(1)))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Bạn đã dùng hết 20 lượt hỏi nhanh trong giờ này. Hãy dành chút thời gian tập luyện và quay lại sau nhé! 💪"
                });
            }


            try
            {
                // =================================================
                // CONTEXT HỘI VIÊN
                // =================================================
                var memberContext =
                    _aiContextBuilder.BuildMemberAIChatContext(
                        userId.Value,
                        question);


                // =================================================
                // CONTEXT SMARTGYM
                // =================================================
                var gymContext =
                    _aiContextBuilder.BuildGymAIChatContext(
                        question);


                // =================================================
                // LỊCH SỬ CHAT
                //
                // Chỉ lấy 6 tin nhắn gần nhất.
                // Mỗi tin tối đa 500 ký tự.
                //
                // Như vậy AI có "trí nhớ" nhưng không đốt
                // quá nhiều token.
                // =================================================
                var history =
    (request.History ?? new List<AIChatHistoryItem>())
    .Where(x =>
        !string.IsNullOrWhiteSpace(x.Content))
    .TakeLast(4)
    .Select(x =>
    {
        var role =
            x.Role == "user"
                ? "Hội viên"
                : "SmartGym AI";

        var content =
            x.Content?.Trim() ?? "";

        if (content.Length > 350)
        {
            content = content[..350];
        }

        return $"{role}: {content}";
    })
    .ToList();


                var historyText =
                    history.Count == 0
                        ? "Chưa có lịch sử trò chuyện."
                        : string.Join(
                            "\n",
                            history);


                // =================================================
                // SERIALIZE COMPACT
                // =================================================
                var memberJson =
                    Newtonsoft.Json.JsonConvert
                        .SerializeObject(
                            memberContext);


                var gymJson =
                    Newtonsoft.Json.JsonConvert
                        .SerializeObject(
                            gymContext);


                // =================================================
                // GỌI AI
                // =================================================
                var result =
                    await _geminiService
                        .ChatWithMemberAsync(
                            question,
                            memberJson,
                            gymJson,
                            historyText);


                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (GeminiServiceException ex)
            {
                return StatusCode(
                    ex.HttpStatusCode,
                    new
                    {
                        success = false,
                        message = ex.Message
                    });
            }
            catch (Exception)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "AI hiện không thể xử lý câu hỏi."
                    });
            }
        }
        // ======================================================
        // HỒ SƠ SỨC KHỎE - GET
        // ======================================================
        [HttpGet]
        [Route("User/HealthProfile")]
        public IActionResult HealthProfile()
        {
            int? userId = GetCurrentMemberId();

            if (userId == null)
                return RedirectToAction("Index", "Home");

            var member = _context.NguoiDungs.FirstOrDefault(x => x.Id == userId);

            if (member == null)
                return NotFound();

            var history = _context.ChiSoSucKhoes
                .Where(x => x.HoiVienId == member.Id)
                .OrderByDescending(x => x.NgayDo)
                .ToList();

            var latest = history.FirstOrDefault();

            var model = new HealthProfileViewModel
            {
                HoiVienId = member.Id,

                HoTen = member.HoTen,

                MaHoiVien = member.MaHoiVien,

                ChieuCao = latest?.ChieuCao ?? member.ChieuCao,

                CanNang = latest?.CanNang ?? member.CanNang,

                VongNguc = latest?.VongNguc,

                VongEo = latest?.VongEo,

                VongHong = latest?.VongHong,

                TinhTrangSucKhoe = latest?.TinhTrangSucKhoe,

                BenhLy = latest?.BenhLy
            };

            model.LichSu = history.Select(x => new HealthHistoryViewModel
            {
                NgayDo = x.NgayDo,
                ChieuCao = x.ChieuCao,
                CanNang = x.CanNang
            }).ToList();

            return View(model);
        }

        // =====================================================
        // HỒ SƠ SỨC KHỎE - POST (LƯU)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("User/HealthProfile")]
        public async Task<IActionResult> HealthProfile(HealthProfileViewModel model)
        {
            var userId = GetCurrentMemberId();

            if (userId == null)
                return RedirectToAction("Index", "Home");

            model.HoiVienId = userId.Value;

            if (!model.ChieuCao.HasValue && !model.CanNang.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập chiều cao hoặc cân nặng.");
            }

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.Where(kvp => kvp.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );

                    return BadRequest(new { success = false, errors });
                }

                // reload history for view
                model.LichSu = await _healthService.GetHistoryAsync(userId.Value, 20);

                return View(model);
            }

            await _healthService.SaveHealthAsync(userId.Value, model);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true });

            TempData["SuccessMessage"] = "Lưu chỉ số sức khỏe thành công.";
            return RedirectToAction("HealthProfile");
        }

        // =====================================================
        // TẢI LỊCH SỬ CHỈ SỐ - JSON
        // =====================================================
        [HttpGet]
        [Route("User/HealthHistory")]
        public async Task<IActionResult> HealthHistory(int? take)
        {
            var userId = GetCurrentMemberId();

            if (userId == null)
                return Json(new { success = false, message = "Unauthorized" });

            int limit = take ?? 20;
            var items = await _healthService.GetHistoryAsync(userId.Value, limit);

            var history = items.Select(x => new
            {
                x.NgayDo,
                x.ChieuCao,
                x.CanNang,
                BMI = (x.ChieuCao.HasValue && x.CanNang.HasValue && x.ChieuCao > 0)
                    ? Math.Round(x.CanNang.Value / Math.Pow(x.ChieuCao.Value / 100.0, 2), 1)
                    : 0
            }).ToList();

            return Json(new { success = true, data = history });
        }
        }
    }
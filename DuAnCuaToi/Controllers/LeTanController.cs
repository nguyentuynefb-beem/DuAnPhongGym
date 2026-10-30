using QRCoder;
using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DuAnCuaToi.Filters;

namespace DuAnCuaToi.Controllers
{
    [SessionRole("LeTan")]
    public class LeTanController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LeTanController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var hoiViens = _context.DangKyGoiTaps
    .Include(x => x.NguoiDung)
    .Include(x => x.GoiTap)
    .Where(x =>
        x.TrangThai &&
        x.NgayHetHan >= DateTime.Now)
    .ToList();

            ViewBag.HoiViens = hoiViens;


            // ========================================
            // DANH SÁCH GÓI CHỜ THANH TOÁN
            // ========================================

            var dangKyChoThanhToan = _context.DangKyGoiTaps
.Include(x => x.NguoiDung)
.Include(x => x.GoiTap)
.Include(x => x.HoaDons)
.Where(x =>
!x.TrangThai &&
x.HoaDons.Any(h =>
h.TrangThai == "Chưa thanh toán" ||
h.TrangThai == "Chờ xác nhận"))
.OrderByDescending(x => x.NgayDangKy)
.ToList()
.Select(x => new
{
x.Id,
x.NguoiDung,
x.GoiTap,
x.NgayDangKy,
HoaDonId = x.HoaDons
.First(h =>
    h.TrangThai == "Chưa thanh toán" ||
    h.TrangThai == "Chờ xác nhận")
.Id
})
.ToList();

            ViewBag.DangKyChoThanhToan = dangKyChoThanhToan;

            return View();
        }
        public IActionResult ThanhToan(int dangKyId)
        {
            // 1. Lấy đăng ký gói
            var dk = _context.DangKyGoiTaps
                .Include(x => x.GoiTap)
                .Include(x => x.NguoiDung)
                .FirstOrDefault(x => x.Id == dangKyId);

            if (dk == null)
            {
                TempData["Error"] = "Không tìm thấy đăng ký gói tập.";
                return RedirectToAction("Index");
            }

            // 2. Kiểm tra thông tin gói
            if (dk.GoiTap == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin gói tập.";
                return RedirectToAction("Index");
            }

            // 3. Nếu gói đã active thì không tạo hóa đơn mới
            if (dk.TrangThai)
            {
                TempData["Error"] = "Gói tập này đã được kích hoạt.";
                return RedirectToAction("Index");
            }

            // 4. Kiểm tra xem đã có hóa đơn chưa thanh toán chưa
            var hoaDonCu = _context.HoaDons
                .FirstOrDefault(x =>
                    x.DangKyGoiTapId == dk.Id &&
                    x.TrangThai == "Chưa thanh toán");

            if (hoaDonCu != null)
            {
                return View("ThanhToan", hoaDonCu);
            }

            // 5. Tạo hóa đơn mới
            HoaDon hd = new HoaDon
            {
                DangKyGoiTapId = dk.Id,
                NgayLap = DateTime.Now,
                TongTien = dk.GoiTap.Gia,
                TrangThai = "Chưa thanh toán"
            };

            _context.HoaDons.Add(hd);

            _context.SaveChanges();

            return View("ThanhToan", hd);
        }
        public IActionResult XacNhanThanhToan(int id)
        {
            // 1. Lấy hóa đơn + đăng ký gói + hội viên + gói tập
            var hd = _context.HoaDons
                .Include(x => x.DangKyGoiTap)
                .ThenInclude(x => x.NguoiDung)
                .Include(x => x.DangKyGoiTap)
                .ThenInclude(x => x.GoiTap)
                .FirstOrDefault(x => x.Id == id);

            if (hd == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction("Index");
            }

            if (hd.DangKyGoiTap == null)
            {
                TempData["Error"] = "Hóa đơn chưa liên kết với gói tập.";
                return RedirectToAction("Index");
            }

            if (hd.DangKyGoiTap.NguoiDung == null)
            {
                TempData["Error"] = "Không tìm thấy hội viên của hóa đơn.";
                return RedirectToAction("Index");
            }

            if (hd.DangKyGoiTap.GoiTap == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin gói tập.";
                return RedirectToAction("Index");
            }

            // 2. Nếu hóa đơn đã thanh toán rồi thì không tạo thanh toán lần nữa
            if (hd.TrangThai == "Đã thanh toán")
            {
                TempData["Error"] = "Hóa đơn này đã được thanh toán.";
                return RedirectToAction("Index");
            }

            // 3. Cập nhật trạng thái hóa đơn
            hd.TrangThai = "Đã thanh toán";

            // 4. Tạo bản ghi thanh toán
            ThanhToan tt = new ThanhToan
            {
                HoaDonId = hd.Id,
                SoTien = hd.TongTien,
                NgayThanhToan = DateTime.Now,
                PhuongThuc = "Chuyển khoản",
                TrangThai = "Đã thanh toán"
            };

            _context.ThanhToans.Add(tt);

            // 5. Kích hoạt gói tập cho hội viên
            var dangKy = hd.DangKyGoiTap;

            dangKy.TrangThai = true;

            // Gói bắt đầu tính từ thời điểm thanh toán
            dangKy.NgayDangKy = DateTime.Now;

            dangKy.NgayHetHan = DateTime.Now
                .AddDays(dangKy.GoiTap.ThoiHanNgay);

            // 6. Tạo thông báo cho hội viên
            var hoiVien = dangKy.NguoiDung;

            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = hoiVien.Id,
                NoiDung =
                    $"Thanh toán thành công {hd.TongTien:N0} VNĐ. " +
                    $"Gói {dangKy.GoiTap.TenGoi} của bạn đã được kích hoạt " +
                    $"đến ngày {dangKy.NgayHetHan:dd/MM/yyyy}.",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            // 7. Lưu tất cả vào database
            _context.SaveChanges();

            TempData["Success"] =
                $"Đã xác nhận thanh toán cho hội viên {hoiVien.HoTen}.";

            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThanhToan(
    int nguoiDungId,
    int goiTapId,
    string phuongThuc)
        {
            // 1. Kiểm tra hội viên
            var hoiVien = _context.NguoiDungs
                .FirstOrDefault(x => x.Id == nguoiDungId);

            if (hoiVien == null)
            {
                TempData["Error"] = "Không tìm thấy hội viên.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Kiểm tra gói tập
            var goi = _context.GoiTaps
                .FirstOrDefault(x => x.Id == goiTapId);

            if (goi == null)
            {
                TempData["Error"] = "Không tìm thấy gói tập.";
                return RedirectToAction(nameof(Index));
            }

            // 3. Chỉ cho phép chuyển khoản
            phuongThuc = "Chuyển khoản";

            // 4. Tạo đăng ký gói tập
            var ngayDangKy = DateTime.Now;
            var ngayHetHan = ngayDangKy.AddDays(goi.ThoiHanNgay);

            var dangKy = new DangKyGoiTap
            {
                NguoiDungId = hoiVien.Id,
                GoiTapId = goi.Id,
                NgayDangKy = ngayDangKy,
                NgayHetHan = ngayHetHan,

                // Thanh toán thành công thì kích hoạt ngay
                TrangThai = true
            };

            _context.DangKyGoiTaps.Add(dangKy);
            _context.SaveChanges();

            // 5. Tạo hóa đơn
            var hoaDon = new HoaDon
            {
                DangKyGoiTapId = dangKy.Id,
                TongTien = goi.Gia,
                NgayLap = DateTime.Now,
                TrangThai = "Đã thanh toán"
            };

            _context.HoaDons.Add(hoaDon);
            _context.SaveChanges();

            // 6. Tạo bản ghi thanh toán
            var thanhToan = new ThanhToan
            {
                HoaDonId = hoaDon.Id,
                SoTien = goi.Gia,
                NgayThanhToan = DateTime.Now,
                PhuongThuc = phuongThuc,
                TrangThai = "Đã thanh toán"
            };

            _context.ThanhToans.Add(thanhToan);

            // 7. Thông báo cho hội viên
            var thongBao = new ThongBao
            {
                NguoiNhanId = hoiVien.Id,
                NoiDung =
                    $"Thanh toán thành công {goi.Gia:N0} VNĐ bằng chuyển khoản. " +
                    $"Gói {goi.TenGoi} của bạn đã được kích hoạt " +
                    $"đến ngày {ngayHetHan:dd/MM/yyyy}.",
                DaDoc = false,
                NgayTao = DateTime.Now
            };

            _context.ThongBaos.Add(thongBao);

            // 8. Lưu toàn bộ
            _context.SaveChanges();

            TempData["Success"] =
                $"Thanh toán thành công cho hội viên {hoiVien.HoTen}.";

            return RedirectToAction(nameof(Index));
        }
        public IActionResult ChiTietHoaDon(int id)
        {
            var hoaDon = _context.HoaDons

                .Include(x => x.DangKyGoiTap)

                .ThenInclude(x => x.NguoiDung)

                .Include(x => x.DangKyGoiTap)

                .ThenInclude(x => x.GoiTap)

                .FirstOrDefault(x => x.Id == id);

            if (hoaDon == null)
                return NotFound();

            return View(hoaDon);
        }
        public IActionResult QRCode(int id)
        {
            var hoaDon = _context.HoaDons

                .Include(x => x.DangKyGoiTap)

                .ThenInclude(x => x.NguoiDung)

                .Include(x => x.DangKyGoiTap)

                .ThenInclude(x => x.GoiTap)

                .FirstOrDefault(x => x.Id == id);

            if (hoaDon == null)
                return NotFound();

            string noiDungQR =
        $@"SMARTGYM
HD:{hoaDon.Id}
HV:{hoaDon.DangKyGoiTap?.NguoiDung?.HoTen}
GOI:{hoaDon.DangKyGoiTap?.GoiTap?.TenGoi}
TIEN:{hoaDon.TongTien:N0}";

            using QRCodeGenerator qrGenerator = new();

            QRCodeData data =
                qrGenerator.CreateQrCode(
                    noiDungQR,
                    QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qr = new(data);

            ViewBag.QRCode =
                Convert.ToBase64String(qr.GetGraphic(20));

            return View(hoaDon);
        }

        public IActionResult QRDiemDanh()
        {
            // Tạo URL mà hội viên sẽ truy cập sau khi quét QR
            string noiDungQR = Url.Action(
                "CheckIn",
                "User",
                null,
                Request.Scheme
            );

            // Tạo mã QR
            using QRCodeGenerator qrGenerator = new();

            QRCodeData data = qrGenerator.CreateQrCode(
                noiDungQR,
                QRCodeGenerator.ECCLevel.Q
            );

            PngByteQRCode qrCode = new(data);

            byte[] bytes = qrCode.GetGraphic(20);

            // Chuyển ảnh QR thành Base64 để hiển thị trực tiếp trong HTML
            ViewBag.QRCode = Convert.ToBase64String(bytes);

            // Cho View biết URL đang nằm trong QR
            ViewBag.CheckInUrl = noiDungQR;

            return View();
        }
        
        [HttpPost]
        public IActionResult CheckInQR([FromBody] string maHoiVien)
        {
            var nguoi =
                _context.NguoiDungs
                .FirstOrDefault(x => x.MaHoiVien == maHoiVien);

            if (nguoi == null)
                return BadRequest();

            CheckIn c = new();

            c.NguoiDungId = nguoi.Id;

            c.ThoiGian = DateTime.Now;

            c.HinhThuc = "QR";

            _context.CheckIns.Add(c);

            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = nguoi.Id,
                NoiDung =
                    $"Điểm danh thành công lúc {c.ThoiGian:HH:mm dd/MM/yyyy}.",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            _context.SaveChanges();

            return Ok();
        }
        public IActionResult EditProfile()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int id))
                return RedirectToAction("Index");

            var user = _context.NguoiDungs.Find(id);

            if (user == null || user.VaiTro != "LeTan")
                return RedirectToAction("Index", "Home");

            var model = new EditProfileViewModel
            {
                Id = user.Id,
                HoTen = user.HoTen,
                TenDangNhap = user.TenDangNhap
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrWhiteSpace(userIdStr) ||
                !int.TryParse(userIdStr, out var sessionUserId) ||
                sessionUserId != model.Id)
            {
                return Forbid();
            }

            var user = _context.NguoiDungs
                .FirstOrDefault(x => x.Id == model.Id && x.VaiTro == "LeTan");

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
                user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.MatKhauMoi);
            }

            _context.SaveChanges();

            HttpContext.Session.SetString("UserName", user.HoTen);
            HttpContext.Session.SetString("Username", user.TenDangNhap);

            TempData["Success"] = "Đã cập nhật.";

            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult CheckIn(string maHoiVien)
        {
            var nguoi = _context.NguoiDungs
                .FirstOrDefault(x => x.MaHoiVien == maHoiVien);

            if (nguoi == null)
            {
                TempData["Error"] = "Không tìm thấy hội viên";
                return RedirectToAction("Index");
            }

            CheckIn check = new CheckIn()
            {
                NguoiDungId = nguoi.Id,
                ThoiGian = DateTime.Now,
                HinhThuc = "Mã hội viên"
            };

            _context.CheckIns.Add(check);

            _context.SaveChanges();
                
            TempData["Success"] = "Điểm danh thành công";

            return RedirectToAction("Index");
        }
    }
}

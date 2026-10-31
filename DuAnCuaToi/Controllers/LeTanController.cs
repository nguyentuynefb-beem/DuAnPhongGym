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
.Select(x =>
{
    var hd = x.HoaDons.First(h => h.TrangThai == "Chưa thanh toán" || h.TrangThai == "Chờ xác nhận");
    return new
    {
        x.Id,
        x.NguoiDung,
        x.GoiTap,
        x.NgayDangKy,
        HoaDonId = hd.Id,
        TrangThaiHoaDon = hd.TrangThai
    };
})
.ToList();

            ViewBag.DangKyChoThanhToan = dangKyChoThanhToan;

            return View();
        }
        [HttpGet]
        public IActionResult ThanhToan(int? hoiVienId = null, int? goiTapId = null)
        {
            ViewBag.HoiViens = _context.NguoiDungs
                .Where(x => x.VaiTro == "HoiVien" && !x.IsDeleted)
                .OrderBy(x => x.HoTen)
                .ToList();

            ViewBag.GoiTaps = _context.GoiTaps
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Gia)
                .ToList();

            ViewBag.SelectedHoiVienId = hoiVienId;
            ViewBag.SelectedGoiTapId = goiTapId;

            return View();
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

            // 5. Kích hoạt gói tập cho hội viên với cơ chế cộng dồn gia hạn
            var dangKy = hd.DangKyGoiTap;
            var hoiVien = dangKy.NguoiDung;

            dangKy.TrangThai = true;
            dangKy.NgayDangKy = DateTime.Now;

            // KIỂM TRA GÓI ĐANG CÒN HẠN ĐỂ CỘNG DỒN
            var goiDangConHan = _context.DangKyGoiTaps
                .Where(x => x.Id != dangKy.Id &&
                            x.NguoiDungId == hoiVien.Id &&
                            x.TrangThai &&
                            x.NgayHetHan > DateTime.Now)
                .OrderByDescending(x => x.NgayHetHan)
                .FirstOrDefault();

            if (goiDangConHan != null)
            {
                dangKy.NgayHetHan = goiDangConHan.NgayHetHan.AddDays(dangKy.GoiTap.ThoiHanNgay);
            }
            else
            {
                dangKy.NgayHetHan = DateTime.Now.AddDays(dangKy.GoiTap.ThoiHanNgay);
            }

            // 6. Tạo thông báo cho hội viên
            string noiDungThongBao = goiDangConHan != null
                ? $"Thanh toán thành công {hd.TongTien:N0} VNĐ. Gói {dangKy.GoiTap.TenGoi} đã được gia hạn cộng dồn thành công, hạn sử dụng mới đến ngày {dangKy.NgayHetHan:dd/MM/yyyy}."
                : $"Thanh toán thành công {hd.TongTien:N0} VNĐ. Gói {dangKy.GoiTap.TenGoi} của bạn đã được kích hoạt đến ngày {dangKy.NgayHetHan:dd/MM/yyyy}.";

            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = hoiVien.Id,
                NoiDung = noiDungThongBao,
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            // 7. Lưu tất cả vào database
            _context.SaveChanges();

            TempData["Success"] = goiDangConHan != null
                ? $"Đã xác nhận thanh toán & gia hạn cộng dồn gói cho hội viên {hoiVien.HoTen} (Hạn đến: {dangKy.NgayHetHan:dd/MM/yyyy})."
                : $"Đã xác nhận thanh toán & kích hoạt gói cho hội viên {hoiVien.HoTen} (Hạn đến: {dangKy.NgayHetHan:dd/MM/yyyy}).";

            return RedirectToAction("Index");
        }

        // =========================================================
        // LỄ TÂN HỦY ĐƠN ĐĂNG KÝ CHỜ THANH TOÁN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyDangKyChoThanhToan(int id)
        {
            var hoaDon = _context.HoaDons
                .Include(x => x.ThanhToans)
                .Include(x => x.DangKyGoiTap)
                    .ThenInclude(x => x!.GoiTap)
                .Include(x => x.DangKyGoiTap)
                    .ThenInclude(x => x!.NguoiDung)
                .FirstOrDefault(x => x.Id == id);

            if (hoaDon == null || hoaDon.DangKyGoiTap == null)
            {
                TempData["Error"] = "Không tìm thấy đơn đăng ký cần hủy.";
                return RedirectToAction(nameof(Index));
            }

            if (hoaDon.TrangThai == "Đã thanh toán")
            {
                TempData["Error"] = "Đơn này đã được thanh toán, không thể hủy.";
                return RedirectToAction(nameof(Index));
            }

            if (hoaDon.ThanhToans.Any(t => t.TrangThai == "Đã thanh toán"))
            {
                TempData["Error"] = "Đơn này đã phát sinh giao dịch thành công, không thể hủy.";
                return RedirectToAction(nameof(Index));
            }

            var dangKy = hoaDon.DangKyGoiTap;
            var hoiVien = dangKy.NguoiDung;
            var tenGoi = dangKy.GoiTap?.TenGoi ?? "gói tập";

            _context.HoaDons.Remove(hoaDon);
            _context.DangKyGoiTaps.Remove(dangKy);

            if (hoiVien != null)
            {
                _context.ThongBaos.Add(new ThongBao
                {
                    NguoiNhanId = hoiVien.Id,
                    NoiDung = $"Lễ tân đã hủy đơn đăng ký {tenGoi} (Hóa đơn #{hoaDon.Id}) của bạn do chưa hoàn tất thanh toán.",
                    DaDoc = false,
                    NgayTao = DateTime.Now
                });
            }

            _context.SaveChanges();

            TempData["Success"] = $"Đã hủy đơn đăng ký {tenGoi} của hội viên {hoiVien?.HoTen ?? "Hội viên"}.";
            return RedirectToAction(nameof(Index));
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
                .FirstOrDefault(x => x.Id == nguoiDungId && !x.IsDeleted);

            if (hoiVien == null)
            {
                TempData["Error"] = "Không tìm thấy hội viên.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Kiểm tra gói tập
            var goi = _context.GoiTaps
                .FirstOrDefault(x => x.Id == goiTapId && !x.IsDeleted);

            if (goi == null)
            {
                TempData["Error"] = "Không tìm thấy gói tập hoặc gói đã ngừng cung cấp.";
                return RedirectToAction(nameof(Index));
            }

            // 3. Chuẩn hóa phương thức thanh toán (Tiền mặt hoặc Chuyển khoản)
            phuongThuc = string.IsNullOrWhiteSpace(phuongThuc) ? "Tiền mặt" : phuongThuc.Trim();

            // 4. Tính toán ngày đăng ký & hạn dùng (có cộng dồn nếu gói cũ còn hạn)
            var ngayDangKy = DateTime.Now;
            DateTime ngayHetHan;

            var goiDangConHan = _context.DangKyGoiTaps
                .Where(x => x.NguoiDungId == hoiVien.Id &&
                            x.TrangThai &&
                            x.NgayHetHan > DateTime.Now)
                .OrderByDescending(x => x.NgayHetHan)
                .FirstOrDefault();

            if (goiDangConHan != null)
            {
                ngayHetHan = goiDangConHan.NgayHetHan.AddDays(goi.ThoiHanNgay);
            }
            else
            {
                ngayHetHan = ngayDangKy.AddDays(goi.ThoiHanNgay);
            }

            // Kiểm tra xem hội viên đã có đăng ký chờ cho gói này chưa
            var dangKyCho = _context.DangKyGoiTaps
                .Include(x => x.HoaDons)
                .FirstOrDefault(x => x.NguoiDungId == hoiVien.Id &&
                                     x.GoiTapId == goi.Id &&
                                     !x.TrangThai);

            DangKyGoiTap dangKy;
            HoaDon hoaDon;

            if (dangKyCho != null)
            {
                dangKy = dangKyCho;
                dangKy.NgayDangKy = ngayDangKy;
                dangKy.NgayHetHan = ngayHetHan;
                dangKy.TrangThai = true;

                hoaDon = dangKy.HoaDons.FirstOrDefault() ?? new HoaDon
                {
                    DangKyGoiTapId = dangKy.Id,
                    TongTien = goi.Gia,
                    NgayLap = DateTime.Now
                };
                hoaDon.TrangThai = "Đã thanh toán";
                if (hoaDon.Id == 0)
                {
                    _context.HoaDons.Add(hoaDon);
                }
            }
            else
            {
                dangKy = new DangKyGoiTap
                {
                    NguoiDungId = hoiVien.Id,
                    GoiTapId = goi.Id,
                    NgayDangKy = ngayDangKy,
                    NgayHetHan = ngayHetHan,
                    TrangThai = true
                };

                _context.DangKyGoiTaps.Add(dangKy);
                _context.SaveChanges();

                hoaDon = new HoaDon
                {
                    DangKyGoiTapId = dangKy.Id,
                    TongTien = goi.Gia,
                    NgayLap = DateTime.Now,
                    TrangThai = "Đã thanh toán"
                };

                _context.HoaDons.Add(hoaDon);
            }

            _context.SaveChanges();

            // 5. Tạo bản ghi thanh toán
            var thanhToan = new ThanhToan
            {
                HoaDonId = hoaDon.Id,
                SoTien = goi.Gia,
                NgayThanhToan = DateTime.Now,
                PhuongThuc = phuongThuc,
                TrangThai = "Đã thanh toán"
            };

            _context.ThanhToans.Add(thanhToan);

            // 6. Thông báo cho hội viên
            string noiDungThongBao = goiDangConHan != null
                ? $"Thanh toán thành công {goi.Gia:N0} VNĐ ({phuongThuc}). Gói {goi.TenGoi} của bạn đã được gia hạn cộng dồn thành công, hạn sử dụng mới đến ngày {ngayHetHan:dd/MM/yyyy}."
                : $"Thanh toán thành công {goi.Gia:N0} VNĐ ({phuongThuc}). Gói {goi.TenGoi} của bạn đã được kích hoạt đến ngày {ngayHetHan:dd/MM/yyyy}.";

            var thongBao = new ThongBao
            {
                NguoiNhanId = hoiVien.Id,
                NoiDung = noiDungThongBao,
                DaDoc = false,
                NgayTao = DateTime.Now
            };

            _context.ThongBaos.Add(thongBao);

            // 7. Lưu toàn bộ
            _context.SaveChanges();

            TempData["Success"] = goiDangConHan != null
                ? $"Thanh toán & gia hạn cộng dồn thành công cho hội viên {hoiVien.HoTen} ({phuongThuc}, Hạn đến: {ngayHetHan:dd/MM/yyyy})."
                : $"Thanh toán & kích hoạt gói thành công cho hội viên {hoiVien.HoTen} ({phuongThuc}, Hạn đến: {ngayHetHan:dd/MM/yyyy}).";

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
            if (string.IsNullOrWhiteSpace(maHoiVien))
            {
                return BadRequest(new { success = false, message = "Mã hội viên không được để trống." });
            }

            maHoiVien = maHoiVien.Trim();

            var nguoi = _context.NguoiDungs
                .FirstOrDefault(x => x.MaHoiVien == maHoiVien && !x.IsDeleted);

            if (nguoi == null)
            {
                return BadRequest(new { success = false, message = $"Không tìm thấy hội viên với mã [{maHoiVien}] hoặc tài khoản đã bị vô hiệu hóa." });
            }

            // 1. Kiểm tra gói tập còn hiệu lực
            var goiTapDangSuDung = _context.DangKyGoiTaps
                .Include(x => x.GoiTap)
                .Where(x => x.NguoiDungId == nguoi.Id && x.TrangThai && x.NgayHetHan >= DateTime.Now)
                .OrderByDescending(x => x.NgayHetHan)
                .FirstOrDefault();

            if (goiTapDangSuDung == null)
            {
                var goiCu = _context.DangKyGoiTaps
                    .Include(x => x.GoiTap)
                    .Where(x => x.NguoiDungId == nguoi.Id && x.TrangThai)
                    .OrderByDescending(x => x.NgayHetHan)
                    .FirstOrDefault();

                string lyDo = goiCu != null
                    ? $"Gói tập [{goiCu.GoiTap?.TenGoi}] của hội viên {nguoi.HoTen} đã hết hạn từ ngày {goiCu.NgayHetHan:dd/MM/yyyy}. Vui lòng gia hạn!"
                    : $"Hội viên {nguoi.HoTen} chưa đăng ký gói tập nào còn hiệu lực. Vui lòng đăng ký gói!";

                return BadRequest(new { success = false, message = lyDo });
            }

            // 2. Kiểm tra đã điểm danh hôm nay chưa
            var batDauNgay = DateTime.Today;
            var ketThucNgay = batDauNgay.AddDays(1);

            var daCheckInHomNay = _context.CheckIns
                .FirstOrDefault(x => x.NguoiDungId == nguoi.Id && x.ThoiGian >= batDauNgay && x.ThoiGian < ketThucNgay);

            if (daCheckInHomNay != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Hội viên {nguoi.HoTen} đã điểm danh hôm nay lúc {daCheckInHomNay.ThoiGian:HH:mm} ({daCheckInHomNay.HinhThuc})."
                });
            }

            // 3. Ghi nhận điểm danh
            CheckIn c = new()
            {
                NguoiDungId = nguoi.Id,
                ThoiGian = DateTime.Now,
                HinhThuc = "QR"
            };

            _context.CheckIns.Add(c);

            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = nguoi.Id,
                NoiDung = $"Điểm danh thành công lúc {c.ThoiGian:HH:mm dd/MM/yyyy} (QR). Gói [{goiTapDangSuDung.GoiTap?.TenGoi}] còn hạn đến {goiTapDangSuDung.NgayHetHan:dd/MM/yyyy}.",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            _context.SaveChanges();

            return Ok(new
            {
                success = true,
                message = $"Điểm danh thành công cho hội viên {nguoi.HoTen} (Gói: {goiTapDangSuDung.GoiTap?.TenGoi}).",
                hoiVien = nguoi.HoTen,
                maHoiVien = nguoi.MaHoiVien,
                tenGoi = goiTapDangSuDung.GoiTap?.TenGoi,
                ngayHetHan = goiTapDangSuDung.NgayHetHan.ToString("dd/MM/yyyy")
            });
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

            _context.SaveChanges();

            HttpContext.Session.SetString("UserName", user.HoTen);
            HttpContext.Session.SetString("Username", user.TenDangNhap);

            TempData["Success"] = "Cập nhật thông tin và mật khẩu thành công.";

            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckIn(string maHoiVien)
        {
            if (string.IsNullOrWhiteSpace(maHoiVien))
            {
                TempData["Error"] = "Vui lòng nhập mã hội viên.";
                return RedirectToAction("Index");
            }

            maHoiVien = maHoiVien.Trim();

            var nguoi = _context.NguoiDungs
                .FirstOrDefault(x => x.MaHoiVien == maHoiVien && !x.IsDeleted);

            if (nguoi == null)
            {
                TempData["Error"] = $"Không tìm thấy hội viên với mã [{maHoiVien}] hoặc tài khoản đã bị vô hiệu hóa.";
                return RedirectToAction("Index");
            }

            // 1. Kiểm tra gói tập còn hiệu lực
            var goiTapDangSuDung = _context.DangKyGoiTaps
                .Include(x => x.GoiTap)
                .Where(x => x.NguoiDungId == nguoi.Id && x.TrangThai && x.NgayHetHan >= DateTime.Now)
                .OrderByDescending(x => x.NgayHetHan)
                .FirstOrDefault();

            if (goiTapDangSuDung == null)
            {
                var goiCu = _context.DangKyGoiTaps
                    .Include(x => x.GoiTap)
                    .Where(x => x.NguoiDungId == nguoi.Id && x.TrangThai)
                    .OrderByDescending(x => x.NgayHetHan)
                    .FirstOrDefault();

                if (goiCu != null)
                {
                    TempData["Error"] = $"Hội viên [{nguoi.HoTen}] đã hết hạn gói [{goiCu.GoiTap?.TenGoi}] từ ngày {goiCu.NgayHetHan:dd/MM/yyyy}. Vui lòng gia hạn gói tập!";
                }
                else
                {
                    TempData["Error"] = $"Hội viên [{nguoi.HoTen}] chưa đăng ký gói tập nào còn hiệu lực. Vui lòng đăng ký gói trước khi vào tập!";
                }

                return RedirectToAction("Index");
            }

            // 2. Kiểm tra đã điểm danh hôm nay chưa
            var batDauNgay = DateTime.Today;
            var ketThucNgay = batDauNgay.AddDays(1);

            var daCheckInHomNay = _context.CheckIns
                .FirstOrDefault(x => x.NguoiDungId == nguoi.Id && x.ThoiGian >= batDauNgay && x.ThoiGian < ketThucNgay);

            if (daCheckInHomNay != null)
            {
                TempData["Error"] = $"Hội viên [{nguoi.HoTen}] đã điểm danh hôm nay lúc {daCheckInHomNay.ThoiGian:HH:mm} (Hình thức: {daCheckInHomNay.HinhThuc})!";
                return RedirectToAction("Index");
            }

            // 3. Ghi nhận điểm danh
            CheckIn check = new()
            {
                NguoiDungId = nguoi.Id,
                ThoiGian = DateTime.Now,
                HinhThuc = "Mã hội viên"
            };

            _context.CheckIns.Add(check);

            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId = nguoi.Id,
                NoiDung = $"Điểm danh thành công lúc {check.ThoiGian:HH:mm dd/MM/yyyy} tại quầy Lễ tân. Gói [{goiTapDangSuDung.GoiTap?.TenGoi}] còn hạn đến {goiTapDangSuDung.NgayHetHan:dd/MM/yyyy}.",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Success"] = $"Điểm danh thành công: [{nguoi.MaHoiVien}] - {nguoi.HoTen} (Gói: {goiTapDangSuDung.GoiTap?.TenGoi}, hạn đến {goiTapDangSuDung.NgayHetHan:dd/MM/yyyy}).";

            return RedirectToAction("Index");
        }
    }
}

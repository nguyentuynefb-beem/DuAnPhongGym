using System;
using System.Collections.Generic;
using System.Linq;
using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DuAnCuaToi.Services
{
    public class AIContextBuilder : IAIContextBuilder
    {
        private readonly ApplicationDbContext _context;

        public AIContextBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // TẠO CONTEXT AI CHO HỘI VIÊN HIỆN TẠI
        // =========================================================
        public AIMemberContext BuildMemberAIContext(int userId)
        {
            var member = _context.NguoiDungs
                .FirstOrDefault(x =>
                    x.Id == userId &&
                    x.VaiTro == "HoiVien");

            if (member == null)
                throw new Exception("Không tìm thấy hội viên.");

            var now = DateTime.Now;

            // ---------------------------------------------------------
            // GÓI TẬP HIỆN TẠI
            // ---------------------------------------------------------
            var currentPackage = _context.DangKyGoiTaps
                .Include(x => x.GoiTap)
                .Where(x =>
                    x.NguoiDungId == userId &&
                    x.TrangThai &&
                    x.NgayHetHan >= now)
                .OrderByDescending(x => x.NgayHetHan)
                .FirstOrDefault();

            AICurrentPackage? packageContext = null;

            if (currentPackage?.GoiTap != null)
            {
                packageContext = new AICurrentPackage
                {
                    TenGoi = currentPackage.GoiTap.TenGoi,
                    Gia = currentPackage.GoiTap.Gia,
                    ThoiHanNgay = currentPackage.GoiTap.ThoiHanNgay,
                    QuyenLoi = currentPackage.GoiTap.QuyenLoi ?? "",
                    NgayDangKy = currentPackage.NgayDangKy,
                    NgayHetHan = currentPackage.NgayHetHan,
                    DangHoatDong = currentPackage.TrangThai,
                    SoNgayConLai =
                        Math.Max(
                            0,
                            (currentPackage.NgayHetHan.Date -
                             now.Date).Days)
                };
            }

            // ---------------------------------------------------------
            // LỊCH SỬ GÓI TẬP
            // ---------------------------------------------------------
            var packageHistory = _context.DangKyGoiTaps
                .Include(x => x.GoiTap)
                .Where(x => x.NguoiDungId == userId)
                .OrderByDescending(x => x.NgayDangKy)
                .Take(10)
                .ToList()
                .Select(x => new AIPackageHistory
                {
                    TenGoi = x.GoiTap?.TenGoi ?? "",
                    Gia = x.GoiTap?.Gia ?? 0,
                    NgayDangKy = x.NgayDangKy,
                    NgayHetHan = x.NgayHetHan,
                    TrangThai = x.TrangThai
                })
                .ToList();

            // ---------------------------------------------------------
            // LỊCH SỬ ĐIỂM DANH
            // ---------------------------------------------------------
            var checkIns = _context.CheckIns
                .Where(x => x.NguoiDungId == userId)
                .OrderByDescending(x => x.ThoiGian)
                .Take(30)
                .ToList()
                .Select(x => new AICheckInData
                {
                    ThoiGian = x.ThoiGian,
                    HinhThuc = x.HinhThuc
                })
                .ToList();

            // ---------------------------------------------------------
            // LỊCH TẬP
            // ---------------------------------------------------------
            var schedules = _context.LichTaps
                .Include(x => x.PT)
                .Where(x => x.HoiVienId == userId)
                .OrderByDescending(x => x.NgayTap)
                .Take(30)
                .ToList()
                .Select(x => new AIScheduleData
                {
                    NgayTap = x.NgayTap,
                    KhungGio = x.KhungGio ?? "",
                    PT = x.PT?.HoTen
                          ?? x.PTUsername
                          ?? "",
                    NoiDung = x.NoiDung ?? "",
                    TrangThai = x.TrangThai,
                    DaDiemDanh = x.DaDiemDanh
                })
                .ToList();

            // ---------------------------------------------------------
            // HÓA ĐƠN / THANH TOÁN
            // ---------------------------------------------------------
            var invoices = _context.HoaDons
                .Include(x => x.DangKyGoiTap)
                    .ThenInclude(x => x.GoiTap)
                .Include(x => x.ThanhToans)
                .Where(x =>
                    x.DangKyGoiTap != null &&
                    x.DangKyGoiTap.NguoiDungId == userId)
                .OrderByDescending(x => x.NgayLap)
                .Take(20)
                .ToList();

            var payments = invoices
                .SelectMany(invoice =>
                    invoice.ThanhToans.Select(payment =>
                        new AIPaymentData
                        {
                            TongTien = invoice.TongTien,
                            NgayLap = invoice.NgayLap,
                            TrangThaiHoaDon =
                                invoice.TrangThai,
                            DaThanhToan =
                                payment.SoTien,
                            PhuongThuc =
                                payment.PhuongThuc
                        }))
                .ToList();

            // ---------------------------------------------------------
            // CHỈ SỐ SỨC KHỎE
            // ---------------------------------------------------------
            var healthData = _context.ChiSoSucKhoes
                .Where(x => x.HoiVienId == userId)
                .OrderByDescending(x => x.NgayDo)
                .Take(20)
                .AsEnumerable()
                .Select(x => new AIHealthData
                {
                    CanNang = x.CanNang,
                    ChieuCao = x.ChieuCao,
                    VongNguc = x.VongNguc,
                    VongEo = x.VongEo,
                    VongHong = x.VongHong,
                    TinhTrangSucKhoe = x.TinhTrangSucKhoe ?? "",
                    BenhLy = x.BenhLy ?? "",
                    NgayDo = x.NgayDo
                })
                .ToList();

            if (!healthData.Any())
            {
                if (member != null)
                {
                    healthData.Add(new AIHealthData
                    {
                        CanNang = member.CanNang,
                        ChieuCao = member.ChieuCao,
                        NgayDo = DateTime.Today
                    });
                }
            }

            // ---------------------------------------------------------
            // TRẢ VỀ CONTEXT
            // ---------------------------------------------------------
            return new AIMemberContext
            {
                HoTen = member.HoTen,
                MaHoiVien = member.MaHoiVien ?? "",
                ChieuCao = member.ChieuCao ?? 0,
                CanNang = member.CanNang ?? 0,
                MucTieuTapLuyen =
                    member.MucTieuTapLuyen ?? "",
                GhiChuSucKhoe =
                    member.GhiChuSucKhoe ?? "",
                PTPhuTrach =
                    member.PTPhuTrach ?? "",
                GoiTapHienTai = packageContext,
                LichSuGoiTap = packageHistory,
                LichSuDiemDanh = checkIns,
                LichTap = schedules,
                ThanhToan = payments,
                ChiSoSucKhoe = healthData
            };
        }

        // =========================================================
        // TẠO CONTEXT CHO KẾ HOẠCH TẬP LUYỆN
        // =========================================================
        public string BuildWorkoutAIContext(int userId)
        {
            var member = _context.NguoiDungs
                .FirstOrDefault(x => x.Id == userId);

            if (member == null)
                return "Không có dữ liệu hội viên.";

            var package = _context.DangKyGoiTaps
                .Include(x => x.GoiTap)
                .Where(x =>
                    x.NguoiDungId == userId &&
                    x.TrangThai)
                .OrderByDescending(x => x.NgayHetHan)
                .FirstOrDefault();

            var recentSchedules = _context.LichTaps
                .Where(x => x.HoiVienId == userId)
                .OrderByDescending(x => x.NgayTap)
                .Take(5)
                .Select(x => new
                {
                    x.NgayTap,
                    x.ThoiGianBatDau,
                    x.ThoiGianKetThuc,
                    x.KhungGio,
                    x.NoiDung,
                    x.TrangThai
                })
                .ToList();

            var context = new
            {
                MucTieu = member.MucTieuTapLuyen,
                ChieuCao = member.ChieuCao ?? 0,
                CanNang = member.CanNang ?? 0,
                GhiChuSucKhoe = member.GhiChuSucKhoe,
                GoiTapHienTai = package == null
                    ? null
                    : new
                    {
                        TenGoi = package.GoiTap?.TenGoi,
                        NgayHetHan = package.NgayHetHan
                    },
                LichTapGanDay = recentSchedules
            };

            return JsonConvert.SerializeObject(context);
        }

        // =========================================================
        // TẠO CONTEXT DỮ LIỆU NỘI BỘ PHÒNG GYM
        // =========================================================
        public AIGymContext BuildGymAIContext()
        {
            var packages = _context.GoiTaps
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Gia)
                .ToList()
                .Select(x => new AIGymPackage
                {
                    MaGoi = x.MaGoi,
                    TenGoi = x.TenGoi,
                    ThoiHanNgay = x.ThoiHanNgay,
                    Gia = x.Gia,
                    QuyenLoi = x.QuyenLoi ?? "",
                    KhuyenMai = x.IsKhuyenMai
                })
                .ToList();

            var services = _context.DichVus
                .OrderBy(x => x.TenDichVu)
                .ToList()
                .Select(x => new AIGymService
                {
                    TenDichVu = x.TenDichVu,
                    MoTa = x.MoTa ?? ""
                })
                .ToList();

            return new AIGymContext
            {
                GoiTap = packages,
                DichVu = services
            };
        }

        // =========================================================
        // CONTEXT AI CHAT GỌN NHẸ
        // =========================================================
        public AIMemberContext BuildMemberAIChatContext(int userId, string question)
        {
            var member = _context.NguoiDungs
                .AsNoTracking()
                .FirstOrDefault(x =>
                    x.Id == userId &&
                    x.VaiTro == "HoiVien");

            if (member == null)
            {
                throw new Exception("Không tìm thấy hội viên.");
            }

            var q = (question ?? "").Trim().ToLowerInvariant();

            bool needIdentity =
                q.Contains("mã hội viên") ||
                q.Contains("ma hoi vien") ||
                q.Contains("tên tôi") ||
                q.Contains("ten toi") ||
                q.Contains("tôi là ai");

            bool needSchedule =
                q.Contains("lịch") ||
                q.Contains("hlv") ||
                q.Contains("pt") ||
                q.Contains("huấn luyện") ||
                q.Contains("trống") ||
                q.Contains("rảnh") ||
                q.Contains("giờ tập");

            bool needHealth =
                q.Contains("inbody") ||
                q.Contains("cân nặng") ||
                q.Contains("chiều cao") ||
                q.Contains("bmi") ||
                q.Contains("sức khỏe") ||
                q.Contains("sức khoẻ") ||
                q.Contains("tiến độ") ||
                q.Contains("cơ thể");

            bool needCheckIn =
                q.Contains("điểm danh") ||
                q.Contains("checkin") ||
                q.Contains("check-in") ||
                q.Contains("đã tập");

            bool needPayment =
                q.Contains("thanh toán") ||
                q.Contains("hóa đơn") ||
                q.Contains("hoá đơn") ||
                q.Contains("tiền") ||
                q.Contains("thanh toan");

            var context = new AIMemberContext
            {
                HoTen = needIdentity ? member.HoTen : "",
                MaHoiVien = needIdentity ? (member.MaHoiVien ?? "") : "",
                MucTieuTapLuyen = member.MucTieuTapLuyen ?? "",
                PTPhuTrach = member.PTPhuTrach ?? ""
            };

            // GÓI HIỆN TẠI
            {
                var now = DateTime.Now;
                var package = _context.DangKyGoiTaps
                    .AsNoTracking()
                    .Include(x => x.GoiTap)
                    .Where(x =>
                        x.NguoiDungId == userId &&
                        x.TrangThai &&
                        x.NgayHetHan >= now)
                    .OrderByDescending(x => x.NgayHetHan)
                    .FirstOrDefault();

                if (package?.GoiTap != null)
                {
                    context.GoiTapHienTai = new AICurrentPackage
                    {
                        TenGoi = package.GoiTap.TenGoi,
                        Gia = package.GoiTap.Gia,
                        ThoiHanNgay = package.GoiTap.ThoiHanNgay,
                        QuyenLoi = package.GoiTap.QuyenLoi ?? "",
                        NgayDangKy = package.NgayDangKy,
                        NgayHetHan = package.NgayHetHan,
                        DangHoatDong = package.TrangThai,
                        SoNgayConLai = Math.Max(0, (package.NgayHetHan.Date - now.Date).Days)
                    };
                }
            }

            // LỊCH TẬP
            if (needSchedule)
            {
                context.LichTap = _context.LichTaps
                    .AsNoTracking()
                    .Include(x => x.PT)
                    .Where(x =>
                        x.HoiVienId == userId &&
                        x.NgayTap >= DateTime.Today.AddDays(-7))
                    .OrderBy(x => x.NgayTap)
                    .ThenBy(x => x.KhungGio)
                    .Take(10)
                    .Select(x => new AIScheduleData
                    {
                        NgayTap = x.NgayTap,
                        KhungGio = x.KhungGio ?? "",
                        PT = x.PT != null ? x.PT.HoTen : (x.PTUsername ?? ""),
                        NoiDung = x.NoiDung ?? "",
                        TrangThai = x.TrangThai,
                        DaDiemDanh = x.DaDiemDanh
                    })
                    .ToList();
            }

            // SỨC KHỎE
            if (needHealth)
            {
                context.ChieuCao = member.ChieuCao ?? 0;
                context.CanNang = member.CanNang ?? 0;
                context.GhiChuSucKhoe = member.GhiChuSucKhoe ?? "";

                context.ChiSoSucKhoe = _context.ChiSoSucKhoes
                    .AsNoTracking()
                    .Where(x => x.HoiVienId == userId)
                    .OrderByDescending(x => x.NgayDo)
                    .Take(5)
                    .Select(x => new AIHealthData
                    {
                        CanNang = x.CanNang,
                        ChieuCao = x.ChieuCao,
                        VongNguc = x.VongNguc,
                        VongEo = x.VongEo,
                        VongHong = x.VongHong,
                        TinhTrangSucKhoe = x.TinhTrangSucKhoe ?? "",
                        BenhLy = x.BenhLy ?? "",
                        NgayDo = x.NgayDo
                    })
                    .ToList();
            }

            // ĐIỂM DANH
            if (needCheckIn)
            {
                context.LichSuDiemDanh = _context.CheckIns
                    .AsNoTracking()
                    .Where(x => x.NguoiDungId == userId)
                    .OrderByDescending(x => x.ThoiGian)
                    .Take(10)
                    .Select(x => new AICheckInData
                    {
                        ThoiGian = x.ThoiGian,
                        HinhThuc = x.HinhThuc
                    })
                    .ToList();
            }

            // THANH TOÁN
            if (needPayment)
            {
                var invoices = _context.HoaDons
                    .AsNoTracking()
                    .Include(x => x.ThanhToans)
                    .Where(x =>
                        x.DangKyGoiTap != null &&
                        x.DangKyGoiTap.NguoiDungId == userId)
                    .OrderByDescending(x => x.NgayLap)
                    .Take(5)
                    .ToList();

                context.ThanhToan = invoices
                    .SelectMany(invoice =>
                        invoice.ThanhToans.Select(payment =>
                            new AIPaymentData
                            {
                                TongTien = invoice.TongTien,
                                NgayLap = invoice.NgayLap,
                                TrangThaiHoaDon = invoice.TrangThai,
                                DaThanhToan = payment.SoTien,
                                PhuongThuc = payment.PhuongThuc
                            }))
                    .ToList();
            }

            return context;
        }

        // =========================================================
        // CONTEXT SMARTGYM CHO FR10
        // =========================================================
        public AIGymContext BuildGymAIChatContext(string question)
        {
            var q = (question ?? "").Trim().ToLowerInvariant();
            var result = new AIGymContext();

            bool needPT =
                q.Contains("hlv") ||
                q.Contains("pt") ||
                q.Contains("huấn luyện") ||
                q.Contains("huan luyen") ||
                q.Contains("trống") ||
                q.Contains("trong") ||
                q.Contains("rảnh") ||
                q.Contains("ranh") ||
                q.Contains("giờ tập") ||
                q.Contains("gio tap") ||
                q.Contains("lịch");

            bool needPackage =
                q.Contains("gói") ||
                q.Contains("goi") ||
                q.Contains("giá") ||
                q.Contains("gia") ||
                q.Contains("khuyến mãi") ||
                q.Contains("khuyen mai") ||
                q.Contains("hạn") ||
                q.Contains("het han") ||
                q.Contains("hết hạn");

            bool needService =
                q.Contains("dịch vụ") ||
                q.Contains("dich vu") ||
                q.Contains("service");

            // GÓI TẬP
            if (needPackage)
            {
                result.GoiTap = _context.GoiTaps
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Gia)
                    .Select(x => new AIGymPackage
                    {
                        MaGoi = x.MaGoi,
                        TenGoi = x.TenGoi,
                        ThoiHanNgay = x.ThoiHanNgay,
                        Gia = x.Gia,
                        QuyenLoi = x.QuyenLoi ?? "",
                        KhuyenMai = x.IsKhuyenMai
                    })
                    .ToList();
            }

            // DỊCH VỤ
            if (needService)
            {
                result.DichVu = _context.DichVus
                    .AsNoTracking()
                    .OrderBy(x => x.TenDichVu)
                    .Select(x => new AIGymService
                    {
                        TenDichVu = x.TenDichVu,
                        MoTa = x.MoTa ?? ""
                    })
                    .ToList();
            }

            // LỊCH TRỐNG HLV
            if (needPT)
            {
                result.LichPTTrong = BuildPTAvailabilityForAI(question);
            }

            return result;
        }

        // =========================================================
        // TÍNH LỊCH TRỐNG HLV CHO AI
        // =========================================================
        public List<AIPTAvailabilityData> BuildPTAvailabilityForAI(string question)
        {
            var result = new List<AIPTAvailabilityData>();
            var q = (question ?? "").Trim().ToLowerInvariant();

            var slots = new[]
            {
                new { Label = "06:00 - 08:00", Start = new TimeSpan(6, 0, 0), End = new TimeSpan(8, 0, 0) },
                new { Label = "08:00 - 10:00", Start = new TimeSpan(8, 0, 0), End = new TimeSpan(10, 0, 0) },
                new { Label = "10:00 - 12:00", Start = new TimeSpan(10, 0, 0), End = new TimeSpan(12, 0, 0) },
                new { Label = "14:00 - 16:00", Start = new TimeSpan(14, 0, 0), End = new TimeSpan(16, 0, 0) },
                new { Label = "16:00 - 18:00", Start = new TimeSpan(16, 0, 0), End = new TimeSpan(18, 0, 0) },
                new { Label = "18:00 - 20:00", Start = new TimeSpan(18, 0, 0), End = new TimeSpan(20, 0, 0) }
            };

            var pts = _context.NguoiDungs
                .AsNoTracking()
                .Where(x => x.VaiTro == "PT" && !x.IsDeleted)
                .Select(x => new { x.Id, x.HoTen, x.TenDangNhap })
                .ToList();

            var requestedPTs = pts
                .Where(pt =>
                    !string.IsNullOrWhiteSpace(pt.HoTen) &&
                    q.Contains(pt.HoTen.Trim().ToLowerInvariant()))
                .ToList();

            if (requestedPTs.Count == 0)
            {
                requestedPTs = pts
                    .Where(pt =>
                    {
                        if (string.IsNullOrWhiteSpace(pt.HoTen))
                            return false;

                        var words = pt.HoTen.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        return words.Length >= 2 && words.Count(word => q.Contains(word)) >= 2;
                    })
                    .ToList();
            }

            if (requestedPTs.Count > 0)
            {
                pts = requestedPTs;
            }

            var from = DateTime.Today;
            var to = from.AddDays(7);

            var bookings = _context.LichTaps
                .AsNoTracking()
                .Where(x =>
                    x.NgayTap >= from &&
                    x.NgayTap < to &&
                    x.TrangThai != "TuChoi")
                .ToList();

            for (int dayIndex = 0; dayIndex < 7; dayIndex++)
            {
                var day = from.AddDays(dayIndex);
                var thu = GetVietnameseDayName(day.DayOfWeek);

                foreach (var pt in pts)
                {
                    var freeSlots = new List<string>();

                    foreach (var slot in slots)
                    {
                        bool busy = bookings.Any(booking =>
                            booking.NgayTap.Date == day.Date &&
                            (booking.MaPT == pt.Id || string.Equals(booking.PTUsername, pt.TenDangNhap, StringComparison.OrdinalIgnoreCase)) &&
                            IsBookingOverlappingSlot(booking, day, slot.Start, slot.End, slot.Label));

                        if (!busy)
                        {
                            freeSlots.Add(slot.Label);
                        }
                    }

                    if (freeSlots.Count > 0)
                    {
                        result.Add(new AIPTAvailabilityData
                        {
                            Ngay = day.ToString("dd/MM/yyyy"),
                            Thu = thu,
                            PT = pt.HoTen,
                            KhungGioTrong = string.Join(", ", freeSlots)
                        });
                    }
                }
            }

            return result;
        }

        private static bool IsBookingOverlappingSlot(
            LichTap booking,
            DateTime day,
            TimeSpan slotStart,
            TimeSpan slotEnd,
            string slotLabel)
        {
            if (!string.IsNullOrWhiteSpace(booking.KhungGio))
            {
                if (string.Equals(booking.KhungGio.Trim(), slotLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (TryParseTimeRange(booking.KhungGio, out TimeSpan bookingStart, out TimeSpan bookingEnd))
                {
                    return bookingStart < slotEnd && bookingEnd > slotStart;
                }
            }

            var actualStart = booking.ThoiGianBatDau.TimeOfDay;
            var actualEnd = booking.ThoiGianKetThuc.TimeOfDay;

            if (actualEnd <= actualStart)
            {
                return false;
            }

            return actualStart < slotEnd && actualEnd > slotStart;
        }

        private static bool TryParseTimeRange(string value, out TimeSpan start, out TimeSpan end)
        {
            start = TimeSpan.Zero;
            end = TimeSpan.Zero;

            var parts = value.Split('-', StringSplitOptions.TrimEntries);
            if (parts.Length != 2) return false;

            bool startOk = TimeSpan.TryParse(parts[0], out start);
            bool endOk = TimeSpan.TryParse(parts[1], out end);

            return startOk && endOk && end > start;
        }

        private static string GetVietnameseDayName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => ""
            };
        }
    }
}

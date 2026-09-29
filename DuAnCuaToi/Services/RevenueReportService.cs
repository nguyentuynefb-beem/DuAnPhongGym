using System;
using System.Collections.Generic;
using System.Linq;
using DuAnCuaToi.Data;
using DuAnCuaToi.Models;
using Microsoft.EntityFrameworkCore;

namespace DuAnCuaToi.Services
{
    public class RevenueReportService : IRevenueReportService
    {
        private readonly ApplicationDbContext _context;

        public RevenueReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public RevenueReportViewModel BuildReport(string loaiKy, DateTime tuNgay, DateTime denNgay)
        {
            var tu = tuNgay.Date;
            var den = denNgay.Date.AddDays(1).AddTicks(-1); // hết ngày denNgay

            // =====================================================
            // 1. LẤY TOÀN BỘ THANH TOÁN "ĐÃ THANH TOÁN" TRONG KỲ
            //    (đọc trực tiếp từ CSDL qua EF Core — đúng chuỗi
            //    trạng thái "Đã thanh toán" đang dùng trong toàn dự án)
            // =====================================================
            var thanhToans = _context.ThanhToans
                .AsNoTracking()
                .Where(t =>
                    t.TrangThai == "Đã thanh toán" &&
                    t.NgayThanhToan >= tu &&
                    t.NgayThanhToan <= den)
                .Include(t => t.HoaDon)
                    .ThenInclude(h => h!.DangKyGoiTap)
                        .ThenInclude(d => d!.GoiTap)
                .ToList();

            // =====================================================
            // 2. GOM NHÓM THEO KỲ (NGÀY / THÁNG / NĂM)
            // =====================================================
            List<RevenueBucketDto> buckets = loaiKy switch
            {
                "Ngay" => thanhToans
                    .GroupBy(t => t.NgayThanhToan.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new RevenueBucketDto
                    {
                        Ky = g.Key.ToString("dd/MM/yyyy"),
                        NgayBatDau = g.Key,
                        SoHoaDon = g.Select(x => x.HoaDonId).Distinct().Count(),
                        DoanhThu = g.Sum(x => x.SoTien)
                    })
                    .ToList(),

                "Nam" => thanhToans
                    .GroupBy(t => t.NgayThanhToan.Year)
                    .OrderBy(g => g.Key)
                    .Select(g => new RevenueBucketDto
                    {
                        Ky = g.Key.ToString(),
                        NgayBatDau = new DateTime(g.Key, 1, 1),
                        SoHoaDon = g.Select(x => x.HoaDonId).Distinct().Count(),
                        DoanhThu = g.Sum(x => x.SoTien)
                    })
                    .ToList(),

                _ => thanhToans // mặc định: Thang
                    .GroupBy(t => new { t.NgayThanhToan.Year, t.NgayThanhToan.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new RevenueBucketDto
                    {
                        Ky = $"{g.Key.Month:D2}/{g.Key.Year}",
                        NgayBatDau = new DateTime(g.Key.Year, g.Key.Month, 1),
                        SoHoaDon = g.Select(x => x.HoaDonId).Distinct().Count(),
                        DoanhThu = g.Sum(x => x.SoTien)
                    })
                    .ToList()
            };

            var tongDoanhThu = thanhToans.Sum(t => t.SoTien);
            var tongSoHoaDon = thanhToans.Select(t => t.HoaDonId).Distinct().Count();
            var soKy = Math.Max(buckets.Count, 1);
            var doanhThuTrungBinh = tongDoanhThu / soKy;

            // =====================================================
            // 3. SO SÁNH VỚI KỲ LIỀN TRƯỚC (CÙNG ĐỘ DÀI THỜI GIAN)
            // =====================================================
            var soNgayTrongKy = (den.Date - tu).Days + 1;
            var tuKyTruoc = tu.AddDays(-soNgayTrongKy);
            var denKyTruoc = tu.AddDays(-1).Date.AddDays(1).AddTicks(-1);

            var doanhThuKyTruoc = _context.ThanhToans
                .Where(t =>
                    t.TrangThai == "Đã thanh toán" &&
                    t.NgayThanhToan >= tuKyTruoc &&
                    t.NgayThanhToan <= denKyTruoc)
                .Sum(t => (decimal?)t.SoTien) ?? 0;

            decimal? tyLeTangTruong = doanhThuKyTruoc > 0
                ? Math.Round(((tongDoanhThu - doanhThuKyTruoc) / doanhThuKyTruoc) * 100, 1)
                : null; // chưa có dữ liệu kỳ trước để so sánh

            // =====================================================
            // 4. TOP GÓI TẬP THEO DOANH THU TRONG KỲ
            // =====================================================
            var topGoiTap = thanhToans
                .Where(t => t.HoaDon?.DangKyGoiTap?.GoiTap != null)
                .GroupBy(t => new
                {
                    t.HoaDon!.DangKyGoiTap!.GoiTap!.Id,
                    t.HoaDon.DangKyGoiTap.GoiTap.TenGoi
                })
                .Select(g => new RevenueTopPackageDto
                {
                    TenGoi = g.Key.TenGoi,
                    SoLuotBan = g.Select(x => x.HoaDon!.DangKyGoiTapId).Distinct().Count(),
                    DoanhThu = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(x => x.DoanhThu)
                .Take(5)
                .ToList();

            return new RevenueReportViewModel
            {
                LoaiKy = loaiKy,
                TuNgay = tu,
                DenNgay = den.Date,
                TongDoanhThu = tongDoanhThu,
                TongSoHoaDon = tongSoHoaDon,
                DoanhThuTrungBinhMoiKy = doanhThuTrungBinh,
                TyLeTangTruongSoVoiKyTruoc = tyLeTangTruong,
                Buckets = buckets,
                TopGoiTap = topGoiTap
            };
        }
    }
}
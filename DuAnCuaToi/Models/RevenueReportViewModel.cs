using System;
using System.Collections.Generic;

namespace DuAnCuaToi.Models
{
    // =========================================================
    // VIEWMODEL CHÍNH CHO TRANG BÁO CÁO DOANH THU (ADMIN)
    // =========================================================
    public class RevenueReportViewModel
    {
        public string LoaiKy { get; set; } = "Thang"; // Ngay | Thang | Nam

        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }

        public decimal TongDoanhThu { get; set; }
        public int TongSoHoaDon { get; set; }
        public decimal DoanhThuTrungBinhMoiKy { get; set; }

        // null nếu chưa đủ dữ liệu kỳ trước để so sánh
        public decimal? TyLeTangTruongSoVoiKyTruoc { get; set; }

        public List<RevenueBucketDto> Buckets { get; set; } = new();
        public List<RevenueTopPackageDto> TopGoiTap { get; set; } = new();
    }

    // Doanh thu của MỘT kỳ (một ngày / một tháng / một năm)
    public class RevenueBucketDto
    {
        public string Ky { get; set; } = "";
        public DateTime NgayBatDau { get; set; }
        public int SoHoaDon { get; set; }
        public decimal DoanhThu { get; set; }
    }

    // Gói tập đóng góp doanh thu trong kỳ báo cáo
    public class RevenueTopPackageDto
    {
        public string TenGoi { get; set; } = "";
        public int SoLuotBan { get; set; }
        public decimal DoanhThu { get; set; }
    }
}
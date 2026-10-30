using System.Collections.Generic;

namespace DuAnCuaToi.Models
{
    // =========================================================
    // DỮ LIỆU GỬI CHO AI ĐỂ TÓM TẮT/PHÂN TÍCH DOANH THU.
    // Chỉ chứa SỐ LIỆU TỔNG HỢP (không có thông tin định danh
    // hội viên) — theo đúng khuôn mẫu AIMemberContext/AIGymContext
    // đã có sẵn trong Models/AIContextModels.cs
    // =========================================================
    public class AIRevenueContext
    {
        public string LoaiKy { get; set; } = "";
        public string TuNgay { get; set; } = "";
        public string DenNgay { get; set; } = "";

        public decimal TongDoanhThu { get; set; }
        public int TongSoHoaDon { get; set; }
        public decimal DoanhThuTrungBinhMoiKy { get; set; }
        public decimal? TyLeTangTruongSoVoiKyTruoc { get; set; }

        public List<RevenueBucketDto> ChiTietTheoKy { get; set; } = new();
        public List<RevenueTopPackageDto> TopGoiTapBanChay { get; set; } = new();
    }
}
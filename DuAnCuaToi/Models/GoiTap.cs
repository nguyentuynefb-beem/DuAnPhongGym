using System.Collections.Generic;

namespace DuAnCuaToi.Models
{
    public class GoiTap
    {
        public int Id { get; set; }

        // =========================
        // THÔNG TIN GÓI
        // =========================
        public string MaGoi { get; set; } = string.Empty;

        public string TenGoi { get; set; } = string.Empty;

        public int ThoiHanNgay { get; set; }

        public decimal Gia { get; set; }

        public string? QuyenLoi { get; set; }

        public bool IsKhuyenMai { get; set; } = false;

        public int? ChiNhanhId { get; set; }

        // =========================
        // DỊCH VỤ ĐI KÈM
        // =========================
        public ICollection<GoiTapDichVu> GoiTapDichVus { get; set; }
            = new List<GoiTapDichVu>();

        // =========================
        // CÁC LẦN ĐĂNG KÝ GÓI
        // =========================
        public ICollection<DangKyGoiTap> DangKyGoiTaps { get; set; }
            = new List<DangKyGoiTap>();
    }
}
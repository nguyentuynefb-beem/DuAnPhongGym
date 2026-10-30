using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DuAnCuaToi.Models
{
    public class NguoiDung
    {
        public int Id { get; set; }

        //=========================
        // THÔNG TIN HỘI VIÊN
        //=========================

        public string? MaHoiVien { get; set; }

        //=========================
        // THÔNG TIN TÀI KHOẢN
        //=========================

        public string HoTen { get; set; } = string.Empty;

        [StringLength(100)]
        public string TenDangNhap { get; set; } = string.Empty;

        public string MatKhauHash { get; set; } = string.Empty;

        // Admin / PT / LeTan / HoiVien
        public string VaiTro { get; set; } = "HoiVien";

        // Giữ để tương thích code cũ
        [NotMapped]
        public string Role
        {
            get => VaiTro;
            set => VaiTro = value;
        }

        //=========================
        // TRẠNG THÁI
        //=========================

        public bool IsOnline { get; set; }

        public DateTime? LastLogin { get; set; }

        public bool IsDeleted { get; set; } = false;

        //=========================
        // THÔNG TIN SỨC KHỎE HIỆN TẠI
        //=========================

        // Giá trị mới nhất
        public double? ChieuCao { get; set; }

        public double? CanNang { get; set; }

        public string? MucTieuTapLuyen { get; set; }

        public string? GhiChuSucKhoe { get; set; }

        //=========================
        // PT PHỤ TRÁCH
        //=========================

        public string? PTPhuTrach { get; set; }

        //=========================
        // Navigation
        //=========================

        public ICollection<ChiSoSucKhoe> LichSuChiSo { get; set; }
            = new List<ChiSoSucKhoe>();
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DuAnCuaToi.Models
{
    public class LichTap
    {
        [Key]
        public int Id { get; set; }

        // =========================
        // HỘI VIÊN
        // =========================
        [Required]
        public int HoiVienId { get; set; }

        // Giữ alias để tương thích code hiện tại
        // Ví dụ UserController đang dùng MaHoiVien.
        [NotMapped]
        public int MaHoiVien
        {
            get => HoiVienId;
            set => HoiVienId = value;
        }

        public virtual NguoiDung? HoiVien { get; set; }

        // =========================
        // PT
        // =========================
        public int? MaPT { get; set; }

        [StringLength(100)]
        public string? PTUsername { get; set; }

        public virtual NguoiDung? PT { get; set; }

        // =========================
        // NGÀY TẬP
        // =========================
        public DateTime NgayTap { get; set; } = DateTime.Now;

        // =========================
        // THỜI GIAN
        // =========================
        public DateTime ThoiGianBatDau { get; set; } = DateTime.Now;

        public DateTime ThoiGianKetThuc { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? KhungGio { get; set; }

        // =========================
        // NỘI DUNG
        // =========================
        [StringLength(500)]
        public string? NoiDung { get; set; }

        // Giữ alias GhiChu cho code cũ.
        [NotMapped]
        public string? GhiChu
        {
            get => NoiDung;
            set => NoiDung = value;
        }

        // =========================
        // ĐIỂM DANH
        // =========================
        public bool DaDiemDanh { get; set; } = false;

        // =========================
        // TRẠNG THÁI
        // =========================
        [StringLength(20)]
        public string TrangThai { get; set; } = "ChoDuyet";

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
using System;
using System.Collections.Generic;

namespace DuAnCuaToi.Models
{
    public class HoaDon
    {
        public int Id { get; set; }

        // =========================
        // ĐĂNG KÝ GÓI TẬP
        // =========================
        public int DangKyGoiTapId { get; set; }

        public DangKyGoiTap? DangKyGoiTap { get; set; }

        // =========================
        // THANH TIỀN
        // =========================
        public decimal TongTien { get; set; }

        // =========================
        // THỜI GIAN
        // =========================
        public DateTime NgayLap { get; set; } = DateTime.Now;

        // =========================
        // TRẠNG THÁI
        // =========================
        public string TrangThai { get; set; } = "Chưa thanh toán";

        // =========================
        // CÁC LẦN THANH TOÁN
        // =========================
        public ICollection<ThanhToan> ThanhToans { get; set; }
            = new List<ThanhToan>();
    }
}
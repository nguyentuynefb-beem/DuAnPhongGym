using System;

namespace DuAnCuaToi.Models
{
    public class ThanhToan
    {
        public int Id { get; set; }

        // =========================
        // HÓA ĐƠN
        // =========================
        public int HoaDonId { get; set; }

        public HoaDon? HoaDon { get; set; }

        // =========================
        // THANH TOÁN
        // =========================
        public decimal SoTien { get; set; }

        public DateTime NgayThanhToan { get; set; } = DateTime.Now;

        public string PhuongThuc { get; set; } = "QR";

        public string TrangThai { get; set; } = "Đã thanh toán";
    }
}
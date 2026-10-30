using System;
using System.Collections.Generic;

namespace DuAnCuaToi.Models
{
    public class DangKyGoiTap
    {
        public int Id { get; set; }

        // =========================
        // HỘI VIÊN
        // =========================
        public int NguoiDungId { get; set; }

        public NguoiDung? NguoiDung { get; set; }

        // =========================
        // GÓI TẬP
        // =========================
        public int GoiTapId { get; set; }

        public GoiTap? GoiTap { get; set; }

        // =========================
        // THỜI GIAN
        // =========================
        public DateTime NgayDangKy { get; set; } = DateTime.Now;

        public DateTime NgayHetHan { get; set; }

        // =========================
        // TRẠNG THÁI
        // =========================
        public bool TrangThai { get; set; } = false;

        // =========================
        // HÓA ĐƠN
        // =========================
        public ICollection<HoaDon> HoaDons { get; set; }
            = new List<HoaDon>();
    }
}
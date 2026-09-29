using System;
using System.Collections.Generic;

namespace DuAnCuaToi.Models
{
    public class PTDashboardViewModel
    {
        // Thông tin chung của HLV
        public string TenHLV { get; set; } = string.Empty;
        public string CaLamViec { get; set; } = "Chưa phân công";

        // Thống kê số liệu
        public int SoBuoiTapHomNay { get; set; }
        public int SoBuoiDaHoanThanh { get; set; }
        public int TongHoiVienQuanLy { get; set; }
        public double DanhGiaTrungBinh { get; set; }
        public int TongLuotDanhGia { get; set; }

        // Danh sách lịch dạy hôm nay
        public List<LichTapItemViewModel> DanhSachLichTap { get; set; } = new();

        // Danh sách hội viên phụ trách (dùng cho dropdown select)
        public List<HoiVienBasicViewModel> DanhSachHoiVien { get; set; } = new();

        // Thông tin chi tiết hội viên đang được chọn xem hồ sơ
        public HoiVienChiTietViewModel? HoiVienChon { get; set; }
    }

    public class LichTapItemViewModel
    {
        public int Id { get; set; }
        public string TenHoiVien { get; set; } = string.Empty;
        public string KhungGio { get; set; } = string.Empty;
        public string NoiDungTap { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty; // "Đã Xong", "Sắp Diễn Ra", ...
    }

    public class HoiVienBasicViewModel
    {
        public int Id { get; set; }
        public string MaHoiVien { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
    }

    public class HoiVienChiTietViewModel
    {
        public int Id { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public double ChieuCao { get; set; }
        public double CanNang { get; set; }
        public double BMI => ChieuCao > 0 ? Math.Round(CanNang / Math.Pow(ChieuCao / 100, 2), 1) : 0;
        public string MucTieu { get; set; } = string.Empty;
        public string TinhTrangSucKhoe { get; set; } = string.Empty;

        // Dữ liệu vẽ biểu đồ cân nặng theo thời gian
        public List<string> LabelsTuan { get; set; } = new();
        public List<double> LichSuCanNang { get; set; } = new();
    }
}
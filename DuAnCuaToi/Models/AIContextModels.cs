namespace DuAnCuaToi.Models
{
    // =========================================================
    // CONTEXT RIÊNG CỦA HỘI VIÊN
    // =========================================================
    public class AIMemberContext
    {
        public string HoTen { get; set; } = "";

        public string MaHoiVien { get; set; } = "";

        public double ChieuCao { get; set; }

        public double CanNang { get; set; }

        public string MucTieuTapLuyen { get; set; } = "";

        public string GhiChuSucKhoe { get; set; } = "";

        public string PTPhuTrach { get; set; } = "";

        public AICurrentPackage? GoiTapHienTai { get; set; }

        public List<AIPackageHistory> LichSuGoiTap { get; set; }
            = new();

        public List<AICheckInData> LichSuDiemDanh { get; set; }
            = new();

        public List<AIScheduleData> LichTap { get; set; }
            = new();

        public List<AIPaymentData> ThanhToan { get; set; }
            = new();

        public List<AIHealthData> ChiSoSucKhoe { get; set; }
            = new();
    }


    // =========================================================
    // GÓI TẬP HIỆN TẠI
    // =========================================================
    public class AICurrentPackage
    {
        public string TenGoi { get; set; } = "";

        public decimal Gia { get; set; }

        public int ThoiHanNgay { get; set; }

        public string QuyenLoi { get; set; } = "";

        public DateTime NgayDangKy { get; set; }

        public DateTime NgayHetHan { get; set; }

        public bool DangHoatDong { get; set; }

        public int SoNgayConLai { get; set; }
    }


    // =========================================================
    // LỊCH SỬ GÓI TẬP
    // =========================================================
    public class AIPackageHistory
    {
        public string TenGoi { get; set; } = "";

        public decimal Gia { get; set; }

        public DateTime NgayDangKy { get; set; }

        public DateTime NgayHetHan { get; set; }

        public bool TrangThai { get; set; }
    }


    // =========================================================
    // ĐIỂM DANH
    // =========================================================
    public class AICheckInData
    {
        public DateTime ThoiGian { get; set; }

        public string HinhThuc { get; set; } = "";
    }


    // =========================================================
    // LỊCH TẬP CỦA HỘI VIÊN
    // =========================================================
    public class AIScheduleData
    {
        public DateTime NgayTap { get; set; }

        public string KhungGio { get; set; } = "";

        public string PT { get; set; } = "";

        public string NoiDung { get; set; } = "";

        public string TrangThai { get; set; } = "";

        public bool DaDiemDanh { get; set; }
    }


    // =========================================================
    // THANH TOÁN
    // =========================================================
    public class AIPaymentData
    {
        public decimal TongTien { get; set; }

        public DateTime NgayLap { get; set; }

        public string TrangThaiHoaDon { get; set; } = "";

        public decimal DaThanhToan { get; set; }

        public string PhuongThuc { get; set; } = "";
    }


    // =========================================================
    // CONTEXT NỘI BỘ SMARTGYM
    // =========================================================
    public class AIGymContext
    {
        public List<AIGymPackage> GoiTap { get; set; }
            = new();

        public List<AIGymService> DichVu { get; set; }
            = new();

        // Lịch trống của HLV trong 7 ngày tới
        public List<AIPTAvailabilityData> LichPTTrong { get; set; }
            = new();
    }


    // =========================================================
    // GÓI TẬP
    // =========================================================
    public class AIGymPackage
    {
        public string MaGoi { get; set; } = "";

        public string TenGoi { get; set; } = "";

        public int ThoiHanNgay { get; set; }

        public decimal Gia { get; set; }

        public string QuyenLoi { get; set; } = "";

        public bool KhuyenMai { get; set; }
    }


    // =========================================================
    // DỊCH VỤ
    // =========================================================
    public class AIGymService
    {
        public string TenDichVu { get; set; } = "";

        public string MoTa { get; set; } = "";
    }


    // =========================================================
    // LỊCH TRỐNG CỦA HLV
    // =========================================================
    public class AIPTAvailabilityData
    {
        public string Ngay { get; set; } = "";

        public string Thu { get; set; } = "";

        public string PT { get; set; } = "";

        public string KhungGioTrong { get; set; } = "";
    }


    // =========================================================
    // REQUEST FR9
    // =========================================================
    public class AIWorkoutRequest
    {
        public string Goal { get; set; } = "";

        public string Availability { get; set; } = "";

        public string Level { get; set; } = "";
    }


    // =========================================================
    // REQUEST FR10
    // =========================================================
        public class AIChatRequest
        {
            public string Question { get; set; } = "";

            public List<AIChatHistoryItem> History { get; set; }
                = new();
        }


        public class AIChatHistoryItem
        {
            public string Role { get; set; } = "";

            public string Content { get; set; } = "";
        }

        // =========================================================
        // CONTEXT HỆ THỐNG DÀNH CHO ADMIN AI
        // =========================================================
        public class AIAdminSystemContext
        {
            public AIAdminUserStats UserStats { get; set; } = new();

            public AIAdminRevenueStats RevenueStats { get; set; } = new();

            public AIAdminOperationsStats OperationsStats { get; set; } = new();

            public List<AIGymPackage> DanhSachGoiTap { get; set; } = new();

            public List<AIAdminPTSummary> DanhSachPT { get; set; } = new();
        }

        public class AIAdminUserStats
        {
            public int TongSoNguoiDung { get; set; }
            public int TongHoiVien { get; set; }
            public int TongPT { get; set; }
            public int TongLeTan { get; set; }
            public int TongAdmin { get; set; }
            public int HoiVienMoiTrongThang { get; set; }
            public int HoiVienDangOnline { get; set; }
            public int HoiVienSapHetHanGoi { get; set; } // Hết hạn trong 7 ngày tới
            public int HoiVienGoiConHan { get; set; }
            public int HoiVienChuaCoGoiHoacHetHan { get; set; }
        }

        public class AIAdminRevenueStats
        {
            public decimal DoanhThuHomNay { get; set; }
            public decimal DoanhThuThangNay { get; set; }
            public decimal DoanhThuThangTruoc { get; set; }
            public double TyLeTangTruongThang { get; set; }
            public int TongHoaDonThangNay { get; set; }
            public int SoHoaDonChoXacNhan { get; set; }
            public List<AIAdminTopPackage> TopGoiTapBanChay { get; set; } = new();
            public List<AIAdminPaymentMethodStat> DoanhThuTheoPhuongThuc { get; set; } = new();
        }

        public class AIAdminTopPackage
        {
            public string TenGoi { get; set; } = "";
            public int SoLuongBan { get; set; }
            public decimal TongDoanhThu { get; set; }
        }

        public class AIAdminPaymentMethodStat
        {
            public string PhuongThuc { get; set; } = "";
            public decimal TongTien { get; set; }
            public int SoGiaoDich { get; set; }
        }

        public class AIAdminOperationsStats
        {
            public int LuotCheckInHomNay { get; set; }
            public int LuotCheckInTuanNay { get; set; }
            public int TongLichTapTuanNay { get; set; }
            public int LichTapChoDuyet { get; set; }
            public int LichTapDaDuyet { get; set; }
            public int LichTapDaHoanThanh { get; set; }
            public int LichTapTuChoi { get; set; }
        }

        public class AIAdminPTSummary
        {
            public int Id { get; set; }
            public string HoTen { get; set; } = "";
            public string TenDangNhap { get; set; } = "";
            public int SoLopDangPhuTrach { get; set; }
            public int SoHocVienDangKem { get; set; }
        }
    }
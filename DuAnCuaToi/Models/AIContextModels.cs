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
    }
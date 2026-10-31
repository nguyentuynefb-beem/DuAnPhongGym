using System.ComponentModel.DataAnnotations;

namespace DuAnCuaToi.ViewModels
{
    public class HealthProfileViewModel
    {
        public int HoiVienId { get; set; }

        public string HoTen { get; set; } = "";

        public string? MaHoiVien { get; set; }

        [Display(Name = "Chiều cao (cm)")]
        [Range(30, 300, ErrorMessage = "Chiều cao phải nằm trong khoảng {1} - {2} cm.")]
        public double? ChieuCao { get; set; }

        [Display(Name = "Cân nặng (kg)")]
        [Range(2, 500, ErrorMessage = "Cân nặng phải nằm trong khoảng {1} - {2} kg.")]
        public double? CanNang { get; set; }

        [Display(Name = "Vòng ngực (cm)")]
        [Range(0, 500, ErrorMessage = "Vòng ngực không hợp lệ.")]
        public double? VongNguc { get; set; }

        [Display(Name = "Vòng eo (cm)")]
        [Range(0, 500, ErrorMessage = "Vòng eo không hợp lệ.")]
        public double? VongEo { get; set; }

        [Display(Name = "Vòng hông (cm)")]
        [Range(0, 500, ErrorMessage = "Vòng hông không hợp lệ.")]
        public double? VongHong { get; set; }

        [Display(Name = "Tình trạng sức khỏe")]
        public string? TinhTrangSucKhoe { get; set; }

        [Display(Name = "Bệnh lý")]
        public string? BenhLy { get; set; }

        public List<HealthHistoryViewModel> LichSu { get; set; } = new();

        // =========================================================
        // CHỈ SỐ INBODY MỞ RỘNG (TÍNH TOÁN TỰ ĐỘNG)
        // =========================================================

        public double BMI
        {
            get
            {
                if (!CanNang.HasValue ||
                    !ChieuCao.HasValue ||
                    ChieuCao <= 0)
                    return 0;

                return Math.Round(
                    CanNang.Value /
                    Math.Pow(ChieuCao.Value / 100.0, 2),
                    1);
            }
        }

        public string DanhGiaBMI
        {
            get
            {
                if (BMI == 0)
                    return "Chưa xác định";

                if (BMI < 18.5)
                    return "Thiếu cân (Gầy)";

                if (BMI < 23.0)
                    return "Bình thường (Cân đối)";

                if (BMI < 25.0)
                    return "Tiền béo phì (Thừa cân)";

                if (BMI < 30.0)
                    return "Béo phì độ I";

                return "Béo phì độ II (Nguy hiểm)";
            }
        }

        // Tỷ lệ Eo / Hông (Waist-to-Hip Ratio - WHR)
        public double? TyLeEoHong
        {
            get
            {
                if (!VongEo.HasValue || !VongHong.HasValue || VongHong.Value <= 0)
                    return null;

                return Math.Round(VongEo.Value / VongHong.Value, 2);
            }
        }

        public string DanhGiaWHR
        {
            get
            {
                if (!TyLeEoHong.HasValue) return "Chưa đủ dữ liệu";

                if (TyLeEoHong.Value <= 0.85) return "Mức an toàn (Cân đối)";
                if (TyLeEoHong.Value <= 0.90) return "Mức trung bình";
                return "Nguy cơ cao (Tích mỡ nội tạng)";
            }
        }

        // Cân nặng lý tưởng theo chuẩn BMI Châu Á (18.5 - 22.9)
        public double? CanNangLyTuongMin
        {
            get
            {
                if (!ChieuCao.HasValue || ChieuCao.Value <= 0) return null;
                double h = ChieuCao.Value / 100.0;
                return Math.Round(18.5 * h * h, 1);
            }
        }

        public double? CanNangLyTuongMax
        {
            get
            {
                if (!ChieuCao.HasValue || ChieuCao.Value <= 0) return null;
                double h = ChieuCao.Value / 100.0;
                return Math.Round(22.9 * h * h, 1);
            }
        }

        public string GoiYCanNang
        {
            get
            {
                if (!CanNang.HasValue || !CanNangLyTuongMin.HasValue || !CanNangLyTuongMax.HasValue)
                    return "Vui lòng nhập chiều cao và cân nặng để nhận phân tích.";

                if (CanNang.Value < CanNangLyTuongMin.Value)
                {
                    double diff = Math.Round(CanNangLyTuongMin.Value - CanNang.Value, 1);
                    return $"Nên tăng khoảng {diff} kg để đạt thể trạng lý tưởng.";
                }

                if (CanNang.Value > CanNangLyTuongMax.Value)
                {
                    double diff = Math.Round(CanNang.Value - CanNangLyTuongMax.Value, 1);
                    return $"Nên giảm khoảng {diff} kg để đạt thể trạng lý tưởng.";
                }

                return "Cân nặng của bạn đang ở mức lý tưởng, hãy tiếp tục duy trì!";
            }
        }

        // Ước tính BMR (Tỷ lệ trao đổi chất cơ bản - kcal/ngày)
        public int? BMR
        {
            get
            {
                if (!CanNang.HasValue || !ChieuCao.HasValue || ChieuCao.Value <= 0 || CanNang.Value <= 0)
                    return null;

                // Công thức Mifflin-St Jeor ước tính
                double bmr = (10 * CanNang.Value) + (6.25 * ChieuCao.Value) - (5 * 25) + 5;
                return (int)Math.Round(bmr);
            }
        }

        // Ước tính TDEE (Năng lượng tiêu hao mỗi ngày khi tập gym 3-5 buổi/tuần)
        public int? TDEE
        {
            get
            {
                if (!BMR.HasValue) return null;
                return (int)Math.Round(BMR.Value * 1.55);
            }
        }
    }
}
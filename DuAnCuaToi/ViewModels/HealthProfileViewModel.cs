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

        public List<HealthHistoryViewModel> LichSu
    = new();

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
                    return "";

                if (BMI < 18.5)
                    return "Thiếu cân";

                if (BMI < 23)
                    return "Bình thường";

                if (BMI < 25)
                    return "Thừa cân";

                if (BMI < 30)
                    return "Béo phì độ I";

                return "Béo phì";
            }
        }
    }
}
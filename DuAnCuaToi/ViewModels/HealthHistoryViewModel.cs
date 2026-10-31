namespace DuAnCuaToi.ViewModels
{
    public class HealthHistoryViewModel
    {
        public DateTime NgayDo { get; set; }

        public double? CanNang { get; set; }

        public double? ChieuCao { get; set; }

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
                if (BMI == 0) return "-";
                if (BMI < 18.5) return "Thiếu cân";
                if (BMI < 23) return "Bình thường";
                if (BMI < 25) return "Thừa cân";
                if (BMI < 30) return "Béo phì độ I";
                return "Béo phì";
            }
        }

        public double? VongNguc { get; set; }

        public double? VongEo { get; set; }

        public double? VongHong { get; set; }

        public double? TyLeEoHong
        {
            get
            {
                if (!VongEo.HasValue || !VongHong.HasValue || VongHong.Value <= 0)
                    return null;
                return Math.Round(VongEo.Value / VongHong.Value, 2);
            }
        }

        public string? TinhTrangSucKhoe { get; set; }

        public string? BenhLy { get; set; }
    }
}
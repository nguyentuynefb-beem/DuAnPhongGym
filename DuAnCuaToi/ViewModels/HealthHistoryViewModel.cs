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

        public double? VongNguc { get; set; }

        public double? VongEo { get; set; }

        public double? VongHong { get; set; }

        public string? TinhTrangSucKhoe { get; set; }

        public string? BenhLy { get; set; }
    }
}
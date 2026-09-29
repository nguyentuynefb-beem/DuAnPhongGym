namespace DuAnCuaToi.Models
{
    public class DichVu
    {
        public int Id { get; set; }
        public string TenDichVu { get; set; } = string.Empty;
        public string? MoTa { get; set; }

        public ICollection<GoiTapDichVu> GoiTapDichVus { get; set; } = new List<GoiTapDichVu>();
    }
}
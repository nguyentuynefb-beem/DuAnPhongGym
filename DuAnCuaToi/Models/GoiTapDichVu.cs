namespace DuAnCuaToi.Models
{
    public class GoiTapDichVu
    {
        public int GoiTapId { get; set; }
        public GoiTap GoiTap { get; set; } = null!;

        public int DichVuId { get; set; }
        public DichVu DichVu { get; set; } = null!;
    }
}
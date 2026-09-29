namespace DuAnCuaToi.Models
{
    public class ThongBao
    {
        public int Id { get; set; }

        public int NguoiNhanId { get; set; }

        public string? NoiDung { get; set; }

        public bool DaDoc { get; set; } = false;

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}

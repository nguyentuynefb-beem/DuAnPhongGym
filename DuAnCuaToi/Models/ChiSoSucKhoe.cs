using System.ComponentModel.DataAnnotations.Schema;

namespace DuAnCuaToi.Models
{
    public class ChiSoSucKhoe
    {
        public int Id { get; set; }

        public int HoiVienId { get; set; }

        [ForeignKey(nameof(HoiVienId))]
        public NguoiDung? HoiVien { get; set; }

        //=========================
        // CHỈ SỐ
        //=========================

        public double? ChieuCao { get; set; }

        public double? CanNang { get; set; }

        public double? VongNguc { get; set; }

        public double? VongEo { get; set; }

        public double? VongHong { get; set; }

        //=========================
        // SỨC KHỎE
        //=========================

        public string? TinhTrangSucKhoe { get; set; }

        public string? BenhLy { get; set; }

        //=========================
        // THỜI GIAN
        //=========================

        public DateTime NgayDo { get; set; }
            = DateTime.Now;
    }
}
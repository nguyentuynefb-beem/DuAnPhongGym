using System;

namespace DuAnCuaToi.Models
{
    public class CheckIn
    {
        public int Id { get; set; }

        public int NguoiDungId { get; set; }

        public NguoiDung? NguoiDung { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        public string HinhThuc { get; set; } = "QR";
    }
}
using System.ComponentModel.DataAnnotations;

namespace DuAnCuaToi.Models
{
    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Required]
        public string HoTen { get; set; }

        [Required]
        public string TenDangNhap { get; set; }

        public string VaiTro { get; set; }

        public string? MatKhauCu { get; set; }

        public string? MatKhauMoi { get; set; }

        public string? XacNhanMatKhau { get; set; }
    }
}
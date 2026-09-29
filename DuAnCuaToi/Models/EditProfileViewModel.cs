using System.ComponentModel.DataAnnotations;

namespace DuAnCuaToi.Models
{
    public class EditProfileViewModel
    {
        public int Id { get; set; }

        [Required]
        public string HoTen { get; set; } = "";

        [Required]
        public string TenDangNhap { get; set; } = "";

        public string? MatKhauMoi { get; set; }

        public string? XacNhanMatKhau { get; set; }
    }
}
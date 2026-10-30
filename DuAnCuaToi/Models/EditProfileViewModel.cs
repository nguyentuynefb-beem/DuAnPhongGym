using System.ComponentModel.DataAnnotations;

namespace DuAnCuaToi.Models
{
    public class EditProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        public string HoTen { get; set; } = "";

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        public string TenDangNhap { get; set; } = "";

        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        public string? MatKhauMoi { get; set; }

        [Compare(nameof(MatKhauMoi), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
        public string? XacNhanMatKhau { get; set; }
    }
}
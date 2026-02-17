using System.ComponentModel.DataAnnotations;

namespace WebBH.Models
{
    public class Register
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        // --- BẮT LỖI MẬT KHẨU TẠI ĐÂY ---
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[\W_]).+$",
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ thường, 1 chữ hoa và 1 ký tự đặc biệt")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }
    }
}
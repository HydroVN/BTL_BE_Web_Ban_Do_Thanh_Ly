using System.ComponentModel.DataAnnotations;

namespace WebBH.Models
{
    public class ResetPasswordVM
    {
        public string Token { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).+$", ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ in hoa và 1 ký tự đặc biệt.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận lại mật khẩu.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }
}
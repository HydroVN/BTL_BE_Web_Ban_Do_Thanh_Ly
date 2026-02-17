using System.ComponentModel.DataAnnotations;

namespace WebBH.Models
{
    public class UserProfile
    {
        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; } // Trường này sẽ để Read-only bên View

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        public string PhoneNumber { get; set; }
    }
}
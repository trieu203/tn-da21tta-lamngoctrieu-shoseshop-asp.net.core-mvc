using System.ComponentModel.DataAnnotations;

namespace khoaLuan_webGiay.ViewModels
{
    public class EmailRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;
    }
}

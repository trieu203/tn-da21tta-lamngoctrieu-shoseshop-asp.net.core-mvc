using System.ComponentModel.DataAnnotations;

namespace khoaLuan_webGiay.ViewModels
{
    public class OtpVerifyRequest
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 chữ số")]
        public string Otp { get; set; } = null!;
    }
}

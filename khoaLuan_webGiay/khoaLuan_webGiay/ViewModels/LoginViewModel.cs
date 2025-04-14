using System.ComponentModel.DataAnnotations;

namespace khoaLuan_webGiay.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
        public string UserNameOrEmail { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        public bool RememberMe { get; set; } = false;
    }
}

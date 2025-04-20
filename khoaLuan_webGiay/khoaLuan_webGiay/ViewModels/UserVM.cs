using System.ComponentModel.DataAnnotations;

namespace khoaLuan_webGiay.ViewModels
{
    public class UserVM
    {
        public int UserId { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public IFormFile? Image { get; set; }

        public bool? Gender { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public string? Role { get; set; }

        public string? ImageUrl { get; set; }
    }

    public class UserEditVM
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }

        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Email phải thuộc miền @gmail.com.")]
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public bool? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? Image { get; set; }
    }
}

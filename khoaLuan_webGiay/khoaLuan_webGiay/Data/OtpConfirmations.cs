using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khoaLuan_webGiay.Data
{
    [Table("OtpConfirmations")]
    public class OtpConfirmations
    {
        [Key]
        [Column("Otp_ID")]
        public int OptId { get; set; }

        public string Email { get; set; } = null!;

        [Column("Otp_Code")]
        public string OtpCode { get; set; } = null!;

        [Column("Created_At")]
        public DateTime CreatedAt { get; set; }

        [Column("Is_Used")]
        public bool IsUsed { get; set; }
    }
}

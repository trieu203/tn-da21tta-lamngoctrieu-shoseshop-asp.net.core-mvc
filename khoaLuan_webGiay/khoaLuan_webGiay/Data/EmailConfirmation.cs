namespace khoaLuan_webGiay.Data;

public partial class EmailConfirmation
{
    public int ConfirmationId { get; set; }

    public int UserId { get; set; }

    public string ConfirmationCode { get; set; } = null!;

    public DateTime ExpirationDate { get; set; }

    public bool IsUsed { get; set; }

    public virtual User User { get; set; } = null!;
}

namespace khoaLuan_webGiay.Data;

public partial class ChatHistory
{
    public int ChatId { get; set; }

    public int? UserId { get; set; }

    public string Message { get; set; } = null!;

    public string? Response { get; set; }

    public DateTime SentAt { get; set; }

    public virtual User? User { get; set; }
}

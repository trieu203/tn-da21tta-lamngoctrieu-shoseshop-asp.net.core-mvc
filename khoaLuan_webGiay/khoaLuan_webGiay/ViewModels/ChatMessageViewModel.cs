namespace khoaLuan_webGiay.ViewModels
{
    public class ChatMessageViewModel
    {
        public int? UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Response { get; set; }
    }
}

namespace khoaLuan_webGiay.Service
{
    public interface IChatbotService
    {
        Task<string> GetResponseAsync(string userMessage, int? userId = null);
    }
}

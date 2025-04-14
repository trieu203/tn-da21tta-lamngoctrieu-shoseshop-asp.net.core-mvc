namespace khoaLuan_webGiay.Service
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string body);
    }
}

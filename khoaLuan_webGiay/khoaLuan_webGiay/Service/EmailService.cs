using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;

namespace khoaLuan_webGiay.Service
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var message = new MailMessage();
            message.To.Add(toEmail);
            message.From = new MailAddress(_settings.UserName, "Milion Sneaker");
            message.Subject = "Xác nhận đăng ký tài khoản - Milion Sneaker";
            message.Body = body;

            using (var client = new SmtpClient(_settings.Host, _settings.Port))
            {
                client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
                client.EnableSsl = _settings.EnableSsl;
                await client.SendMailAsync(message);
            }
        }
    }
}

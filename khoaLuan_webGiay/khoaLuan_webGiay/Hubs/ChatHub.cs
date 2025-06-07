using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.Service;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace khoaLuan_webGiay.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatbotService _chatbotService;
        private readonly KhoaLuanContext _context;

        public ChatHub(IChatbotService chatbotService, KhoaLuanContext context)
        {
            _chatbotService = chatbotService;
            _context = context;
        }

        // Client gửi message lên
        public async Task SendMessage(string message)
        {
            try
            {
                var userIdStr = Context.User?.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        sender = "bot",
                        message = "⚠️ Bạn cần đăng nhập để sử dụng chatbot (UserId không tồn tại).",
                        sentAt = DateTime.Now
                    });
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        sender = "bot",
                        message = "⚠️ Bạn cần nhập tin nhắn.",
                        sentAt = DateTime.Now
                    });
                    return;
                }

                var response = await _chatbotService.GetResponseAsync(message, userId);
                var now = DateTime.Now;

                // Lưu user
                _context.ChatHistories.Add(new ChatHistory
                {
                    UserId = userId,
                    Message = message,
                    Sender = "user",
                    SentAt = now
                });

                // Lưu bot
                _context.ChatHistories.Add(new ChatHistory
                {
                    UserId = userId,
                    Message = response,
                    Sender = "bot",
                    SentAt = now.AddMilliseconds(1)
                });

                await _context.SaveChangesAsync();

                // Gửi lại tin nhắn
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    sender = "user",
                    message = message,
                    sentAt = now
                });

                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    sender = "bot",
                    message = response,
                    sentAt = now.AddMilliseconds(1)
                });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    sender = "bot",
                    message = "⚠️ Lỗi server: " + ex.Message,
                    sentAt = DateTime.Now
                });
            }
        }
    }
}
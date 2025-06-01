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
                Console.WriteLine("👉 Bắt đầu SendMessage");

                var userIdStr = Context.User?.FindFirst("UserId")?.Value;
                Console.WriteLine("🔍 UserId claim = " + userIdStr);
                Console.WriteLine("✅ IsAuthenticated: " + Context.User?.Identity?.IsAuthenticated);

                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        response = "⚠️ Bạn cần đăng nhập để sử dụng chatbot (UserId không tồn tại)."
                    });
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", new
                    {
                        response = "⚠️ Bạn cần nhập tin nhắn."
                    });
                    return;
                }

                var response = await _chatbotService.GetResponseAsync(message, userId);

                var chatHistory = new ChatHistory
                {
                    UserId = userId,
                    Message = message,
                    Response = response,
                    SentAt = DateTime.Now
                };

                _context.ChatHistories.Add(chatHistory);
                await _context.SaveChangesAsync();

                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    userId,
                    message,
                    response,
                    sentAt = chatHistory.SentAt
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ LỖI ChatHub:");
                Console.WriteLine("Message: " + ex.Message);
                Console.WriteLine("StackTrace: " + ex.StackTrace);

                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    response = "⚠️ Lỗi server: " + ex.Message
                });
            }
        }
    }
}
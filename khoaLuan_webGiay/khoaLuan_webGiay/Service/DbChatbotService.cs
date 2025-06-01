using khoaLuan_webGiay.ChatKeyWords;
using khoaLuan_webGiay.Data;
using Microsoft.EntityFrameworkCore;
using khoaLuan_webGiay.Helpers;


namespace khoaLuan_webGiay.Service
{
    public class DbChatbotService : IChatbotService
    {
        private readonly KhoaLuanContext _context;

        public DbChatbotService(KhoaLuanContext context)
        {
            _context = context;
        }

        public async Task<string> GetResponseAsync(string userMessage, int? userId = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return "Bạn vui lòng nhập câu hỏi.";

            userMessage = userMessage.ToLower();

            // Ưu tiên phản hồi theo từ khóa tĩnh
            var matchedResponse = MatchKeyword(userMessage);
            if (!string.IsNullOrEmpty(matchedResponse))
                return matchedResponse;

            // Kiểm tra hãng giày được nhắc tới trong câu hỏi
            var matchedCategory = await GetMatchedCategory(userMessage);
            if (matchedCategory != null)
            {
                // Truy vấn sản phẩm theo hãng, để trả về số lượng và giá rẻ nhất
                var products = await _context.Products
                    .Where(p => p.CategoryId == matchedCategory.CategoryId)
                    .OrderBy(p => p.Price)
                    .ToListAsync();

                if (!products.Any())
                    return $"Hiện chưa có sản phẩm nào thuộc hãng {matchedCategory.CategoryName}.";

                var cheapest = products.First();
                return $"Hãng {matchedCategory.CategoryName} hiện có {products.Count} sản phẩm. " +
                       $"Giá rẻ nhất là {cheapest.Price:N0} VNĐ với sản phẩm \"{cheapest.ProductName}\".";
            }

            // Liệt kê các hãng
            if (userMessage.Contains("hãng") || userMessage.Contains("thương hiệu"))
            {
                var brands = await _context.Categories.Select(c => c.CategoryName).ToListAsync();
                return brands.Any()
                    ? $"Chúng tôi hiện đang phân phối các hãng giày: {string.Join(", ", brands)}."
                    : "Hiện tại chưa có hãng giày nào được cập nhật.";
            }

            // Hỏi giá sản phẩm cụ thể
            if (userMessage.Contains("giá") && userMessage.Contains("adidas"))
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower().Contains("adidas"));

                return product != null
                    ? $"Giá của {product.ProductName} là {product.Price:N0} VNĐ."
                    : "Hiện chúng tôi không có sản phẩm Adidas.";
            }
            if (userMessage.Contains("giá") && userMessage.Contains("nike"))
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower().Contains("nike"));

                return product != null
                    ? $"Giá của {product.ProductName} là {product.Price:N0} VNĐ."
                    : "Hiện chúng tôi không có sản phẩm Nike.";
            }
            if (userMessage.Contains("giá") && userMessage.Contains("puma"))
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower().Contains("puma"));

                return product != null
                    ? $"Giá của {product.ProductName} là {product.Price:N0} VNĐ."
                    : "Hiện chúng tôi không có sản phẩm Puma.";
            }
            if (userMessage.Contains("giá") && userMessage.Contains("new balance"))
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower().Contains("new balance"));

                return product != null
                    ? $"Giá của {product.ProductName} là {product.Price:N0} VNĐ."
                    : "Hiện chúng tôi không có sản phẩm New Balance.";
            }
            if (userMessage.Contains("giá") && userMessage.Contains("onitsuka tiger"))
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower().Contains("onitsuka tiger"));

                return product != null
                    ? $"Giá của {product.ProductName} là {product.Price:N0} VNĐ."
                    : "Hiện chúng tôi không có sản phẩm Onitsuka Tiger.";
            }

            // Hỏi về đơn hàng của người dùng
            if (userMessage.Contains("đơn hàng") && userId.HasValue)
            {
                var order = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .FirstOrDefaultAsync();

                if (order != null)
                {
                    var statusVn = OrderStatusTranslator.Translate(order.OrderStatus);
                    return $"Đơn hàng gần nhất của bạn (mã #{order.OrderId}) đang ở trạng thái: {statusVn}.";
                }
                else
                {
                    return "Bạn chưa có đơn hàng nào.";
                }
            }

            // Hỏi danh sách các hãng giày
            if (userMessage.Contains("hãng") || userMessage.Contains("thương hiệu") ||
                userMessage.Contains("loại giày") || userMessage.Contains("mấy hãng") || userMessage.Contains("có hãng nào") || userMessage.Contains("những hãng"))
            {
                var brands = await _context.Categories
                    .Where(c => !string.IsNullOrEmpty(c.CategoryName))
                    .Select(c => c.CategoryName)
                    .ToListAsync();

                return brands.Any()
                    ? $"Chúng tôi hiện đang phân phối các hãng giày: {string.Join(", ", brands)}."
                    : "Hiện tại chưa có hãng giày nào được cập nhật.";
            }


            return "Cảm ơn bạn đã liên hệ! Tôi có thể giúp gì cho bạn?";
        }

        private string? MatchKeyword(string message)
        {
            foreach (var pair in ChatbotKnowledgeBase.KeywordResponses)
            {
                if (pair.Key.Any(k => message.Contains(k)))
                    return pair.Value;
            }
            return null;
        }

        private async Task<Category?> GetMatchedCategory(string userMessage)
        {
            var matched = await _context.Categories
                .Where(c => !string.IsNullOrEmpty(c.CategoryName) && userMessage.Contains(c.CategoryName.ToLower()))
                .Select(c => new { c.CategoryId, c.CategoryName })
                .FirstOrDefaultAsync();

            if (matched == null) return null;

            return new Category
            {
                CategoryId = matched.CategoryId,
                CategoryName = matched.CategoryName
            };
        }
    }
}

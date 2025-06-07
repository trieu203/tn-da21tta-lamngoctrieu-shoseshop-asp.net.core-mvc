using khoaLuan_webGiay.ChatKeyWords;
using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.Helpers;
using khoaLuan_webGiay.Service;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

public class ChatbotPatternRule
{
    public Func<string, bool> IsMatch { get; set; } = _ => false;
    public Func<string, int?, Task<string>> HandleAsync { get; set; } = (_, _) => Task.FromResult("");
}
public class DbChatbotService : IChatbotService
{
    private readonly KhoaLuanContext _context;
    private List<ChatbotPatternRule>? _rules;

    public DbChatbotService(KhoaLuanContext context)
    {
        _context = context;
    }

    public async Task<string> GetResponseAsync(string userMessage, int? userId = null)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return "Bạn vui lòng nhập nội dung cần hỗ trợ.";

        userMessage = userMessage.ToLower().Trim();

        if (_rules == null) InitializeRules();

        foreach (var rule in _rules!)
        {
            if (rule.IsMatch(userMessage))
            {
                var response = await rule.HandleAsync(userMessage, userId);
                if (!string.IsNullOrEmpty(response))
                    return response;
            }
        }

        return "Xin lỗi, tôi chưa hiểu rõ yêu cầu của bạn. Bạn có thể hỏi lại hoặc cung cấp thêm thông tin nhé!";
    }
    private void InitializeRules()
    {
        _rules = new List<ChatbotPatternRule>
        {
            new ChatbotPatternRule
            {
                IsMatch = msg => Regex.IsMatch(msg, @"(bàn chân|chân).*?(\d{2}(\.\d+)?)(cm)?"),
                HandleAsync = async (msg, userId) =>
                {
                    var match = Regex.Match(msg, @"(bàn chân|chân).*?(\d{2}(\.\d+)?)(cm)?");
                    if (match.Success && double.TryParse(match.Groups[2].Value, out double footLength))
                        return SuggestSizeByFootLength(footLength);
                    return "";
                }
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => !string.IsNullOrEmpty(MatchKeyword(msg)),
                HandleAsync = (msg, userId) => Task.FromResult(MatchKeyword(msg)!)
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => msg.Contains("đơn hàng"),
                HandleAsync = async (msg, userId) => userId.HasValue
                    ? await GetLastOrderStatus(userId.Value)
                    : "Bạn cần đăng nhập để xem đơn hàng."
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => msg.Contains("yêu thích") || msg.Contains("đã lưu"),
                HandleAsync = async (msg, userId) => userId.HasValue
                    ? await ListFavoriteProducts(userId.Value)
                    : "Bạn cần đăng nhập để xem danh sách yêu thích."
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => Regex.IsMatch(msg, @"(hãng|thương hiệu|loại giày)"),
                HandleAsync = async (msg, userId) => await ListBrands()
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => Regex.IsMatch(msg, @"(đánh giá cao|nhiều sao|tốt nhất)"),
                HandleAsync = async (msg, userId) => await ListTopRatedProducts()
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => Regex.IsMatch(msg, @"(xem nhiều|phổ biến|quan tâm)"),
                HandleAsync = async (msg, userId) => await ListMostViewedProducts()
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => msg.Contains("size") || msg.Contains("bao nhiêu size") || msg.Contains("cỡ giày"),
                HandleAsync = async (msg, userId) => await GetSizeAvailability(msg)
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => Regex.IsMatch(msg, @"(bán|có).*(loại|kiểu|dòng).*giày") ||
                                 Regex.IsMatch(msg, @"shop.*(giày gì|loại giày)") ||
                                 Regex.IsMatch(msg, @"(giày nào|loại nào)") ||
                                 Regex.IsMatch(msg, @"(có không|giày không)"),
                HandleAsync = async (msg, userId) => await ListProductCategories()
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => Regex.IsMatch(msg, @"(tất cả|liệt kê|các).*giày"),
                HandleAsync = async (msg, userId) => await ListAllProducts()
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => Regex.IsMatch(msg, @"(rẻ nhất|giá thấp nhất)"),
                HandleAsync = async (msg, userId) => await GetCheapestProduct()
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => Regex.IsMatch(msg, @"(đắt nhất|giá cao nhất)"),
                HandleAsync = async (msg, userId) => await GetMostExpensiveProduct()
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => Regex.IsMatch(msg, @"(?:dưới|<=|ít hơn|bé hơn)\s*(\d{4,9})"),
                HandleAsync = async (msg, userId) => await HandlePriceQuery(msg)
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => true, // fallback để check hãng theo tên
                HandleAsync = async (msg, userId) => await HandleBrandQuery(msg) ?? ""
            },
            new ChatbotPatternRule
            {
                IsMatch = msg => msg.Contains("giày "),
                HandleAsync = async (msg, userId) => await HandleBrandQuery(msg) ?? "Hiện chưa có thông tin về hãng bạn hỏi."
            },

        };
    }

    private string SuggestSizeByFootLength(double length) =>
        length switch
        {
            <= 24.0 => "👣 Với chiều dài chân khoảng 24cm, bạn nên chọn Size 39 nhé!",
            > 24.0 and <= 25.0 => "👣 Với chiều dài chân khoảng 25cm, bạn nên chọn Size 40 nhé!",
            > 25.0 and <= 26.0 => "👣 Với chiều dài chân khoảng 26cm, bạn nên chọn Size 41 nhé!",
            _ => "Hiện tại shop chỉ có các size từ 39 đến 41. Bạn có thể liên hệ để đặt size phù hợp hơn."
        };

    private async Task<string> GetCheapestProduct()
    {
        var product = await _context.Products
            .OrderBy(p => p.Price * (100 - p.Discount) / 100)
            .FirstOrDefaultAsync();

        if (product == null)
            return "Hiện tại chưa có sản phẩm nào.";

        var finalPrice = product.Price * (100 - product.Discount) / 100;
        return $"👟 Đôi giày rẻ nhất hiện tại là:\n" +
               $"- {product.ProductName}: {finalPrice:N0} VNĐ (giá gốc {product.Price:N0})\n" +
               $"👉 <a href='/Products/Details/{product.ProductId}' target='_blank'>Xem chi tiết</a>";
    }

    private async Task<string> GetMostExpensiveProduct()
    {
        var product = await _context.Products
            .OrderByDescending(p => p.Price * (100 - p.Discount) / 100)
            .FirstOrDefaultAsync();

        if (product == null)
            return "Hiện tại chưa có sản phẩm nào.";

        var finalPrice = product.Price * (100 - product.Discount) / 100;
        return $"👟 Đôi giày đắt nhất hiện tại là:\n" +
               $"- {product.ProductName}: {finalPrice:N0} VNĐ (giá gốc {product.Price:N0})\n" +
               $"👉 <a href='/Products/Details/{product.ProductId}' target='_blank'>Xem chi tiết</a>";
    }

    private async Task<string> ListProductCategories()
    {
        var categories = await _context.Categories
            .Where(c => !string.IsNullOrEmpty(c.CategoryName))
            .Select(c => c.CategoryName)
            .ToListAsync();

        if (!categories.Any())
            return "Hiện tại shop chưa cập nhật danh mục giày nào.";

        return "Shop hiện đang bán các loại giày sau:\n- " + string.Join("\n- ", categories);
    }

    private async Task<string> ListAllProducts()
    {
        var products = await _context.Products
            .OrderBy(p => p.ProductName)
            .Take(10) // Giới hạn để tránh quá dài
            .ToListAsync();

        if (!products.Any())
            return "Hiện tại chưa có sản phẩm giày nào trong hệ thống.";

        var result = products.Select(p =>
        {
            var finalPrice = p.Price * (100 - p.Discount) / 100;
            return $"- {p.ProductName}: {finalPrice:N0} VNĐ (giá gốc {p.Price:N0})";
        });

        return $"Dưới đây là một số sản phẩm giày hiện có:\n{string.Join("\n", result)}\nBạn có thể truy cập danh sách đầy đủ tại trang Sản phẩm.";
    }

    private string? MatchKeyword(string message)
    {
        foreach (var pair in ChatbotKnowledgeBase.KeywordResponses)
            if (pair.Key.Any(k => message.Contains(k))) return pair.Value;
        return null;
    }

    private async Task<string?> HandleBrandQuery(string msg)
    {
        // Lấy tất cả tên hãng từ CSDL
        var allCategories = await _context.Categories
            .Where(c => !string.IsNullOrEmpty(c.CategoryName))
            .ToListAsync();

        // Tìm hãng nào xuất hiện trong câu hỏi (so sánh chữ thường)
        var matchedCategory = allCategories
            .FirstOrDefault(c => msg.Contains(c.CategoryName.ToLower()));

        // Nếu không tìm được hãng nào
        if (matchedCategory == null)
            return "Hiện chưa có sản phẩm nào thuộc hãng bạn vừa hỏi.";

        // Lấy tất cả sản phẩm thuộc hãng đó
        var products = await _context.Products
            .Where(p => p.CategoryId == matchedCategory.CategoryId)
            .OrderBy(p => p.Price * (100 - p.Discount) / 100)
            .ToListAsync();

        if (!products.Any())
            return $"Hiện chưa có sản phẩm nào thuộc hãng {matchedCategory.CategoryName}.";

        var cheapest = products.First();
        var mostExpensive = products.Last(); // đã được OrderBy(p => p.Price)

        var productList = products.Take(5).Select(p =>
        {
            var finalPrice = p.Price * (100 - p.Discount) / 100;
            return $"- <a href='/Products/Details/{p.ProductId}' target='_blank'>{p.ProductName}</a>: {finalPrice:N0} VNĐ";
        });

        var finalResponse = $"🧾 Shop hiện có {products.Count} sản phẩm thuộc hãng <b>{matchedCategory.CategoryName}</b>:<br>" +
                            $"- Rẻ nhất: <a href='/Products/Details/{cheapest.ProductId}' target='_blank' style='color: #007bff;'>{cheapest.ProductName}</a> giá {cheapest.Price:N0} VNĐ<br>" +
                            $"- Đắt nhất: <a href='/Products/Details/{mostExpensive.ProductId}' target='_blank' style='color: #007bff;'>{mostExpensive.ProductName}</a> giá {mostExpensive.Price:N0} VNĐ<br><br>" +
                            "🛍️ Một số sản phẩm tiêu biểu:<br>" +
                            string.Join("<br>", productList) +
                            $"<br><br>👉 <a href='/Products?categoryId={matchedCategory.CategoryId}' target='_blank'>Xem tất cả</a>";

        return finalResponse;
    }

    private async Task<string?> HandlePriceQuery(string msg)
    {
        var regex = new Regex(@"(?:dưới|<=|ít hơn|bé hơn)\s*(\d{4,9})", RegexOptions.IgnoreCase);
        var match = regex.Match(msg);

        if (match.Success && int.TryParse(match.Groups[1].Value, out int maxPrice))
        {
            var matchedProducts = await _context.Products
                .Where(p => (p.Price * (100 - p.Discount) / 100) <= maxPrice)
                .OrderBy(p => p.Price)
                .Take(5)
                .ToListAsync();

            if (!matchedProducts.Any())
                return $"Không có sản phẩm nào dưới {maxPrice:N0} VNĐ.";

            var results = matchedProducts.Select(p =>
            {
                var finalPrice = p.Price * (100 - p.Discount) / 100;
                return $"- {p.ProductName}:\n  🔹 Giá gốc: {p.Price:N0} VNĐ\n  🔻 Giá sau giảm: {finalPrice:N0} VNĐ";
            });

            return $"Một số sản phẩm dưới {maxPrice:N0} VNĐ:\n{string.Join("\n", results)}";
        }

        return null;
    }

    private async Task<string> GetSizeAvailability(string msg)
    {
        var product = await _context.Products
            .Where(p => !string.IsNullOrEmpty(p.ProductName) && msg.Contains(p.ProductName.ToLower()))
            .Select(p => new { p.ProductId, p.ProductName })
            .FirstOrDefaultAsync();

        if (product == null) return "Mình chưa xác định được sản phẩm bạn đang hỏi. Bạn có thể ghi rõ tên giày hơn được không?";

        var sizes = await _context.ProductSizes
            .Where(ps => ps.ProductId == product.ProductId && ps.Quantity > 0)
            .OrderBy(ps => ps.Size)
            .Select(ps => new { ps.Size, ps.Quantity })
            .ToListAsync();

        return !sizes.Any()
            ? $"Hiện tại sản phẩm \"{product.ProductName}\" đang hết size hoặc chưa cập nhật size."
            : $"Sản phẩm \"{product.ProductName}\" hiện có các size: {string.Join(", ", sizes.Select(s => $"{s.Size} ({s.Quantity} đôi)"))}.";
    }
    private async Task<string> ListBrands()
    {
        var brands = await _context.Categories
            .Where(c => !string.IsNullOrEmpty(c.CategoryName))
            .Select(c => c.CategoryName)
            .ToListAsync();

        return brands.Any()
            ? $"Chúng tôi hiện đang phân phối các hãng giày: {string.Join(", ", brands)}."
            : "Hiện tại chưa có hãng giày nào được cập nhật.";
    }

    private async Task<string> ListFavoriteProducts(int userId)
    {
        var favorites = await _context.WishLists
            .Where(w => w.UserId == userId)
            .Select(w => new
            {
                w.Product.ProductName,
                w.Product.Price,
                w.Product.Discount
            }).ToListAsync();

        if (!favorites.Any())
            return "Bạn chưa có sản phẩm nào trong danh sách yêu thích.";

        var results = favorites.Select(p =>
        {
            var finalPrice = p.Price * (100 - p.Discount) / 100;
            return $"- {p.ProductName}: {finalPrice:N0} VNĐ (giá gốc {p.Price:N0})";
        });

        return $"Sản phẩm bạn yêu thích:\n{string.Join("\n", results)}";
    }

    private async Task<string> ListTopRatedProducts()
    {
        var products = await _context.Products
            .OrderByDescending(p => p.AverageRating)
            .Take(5)
            .ToListAsync();

        if (!products.Any())
            return "Hiện chưa có sản phẩm nào được đánh giá.";

        var results = products.Select(p =>
        {
            var finalPrice = p.Price * (100 - p.Discount) / 100;
            return $"- {p.ProductName} (⭐ {p.AverageRating:N1}) còn {finalPrice:N0} VNĐ";
        });

        return $"Các sản phẩm được đánh giá cao:\n{string.Join("\n", results)}";
    }

    private async Task<string> ListMostViewedProducts()
    {
        var products = await _context.Products
            .OrderByDescending(p => p.ViewCount)
            .Take(5)
            .ToListAsync();

        if (!products.Any())
            return "Chưa có sản phẩm được quan tâm nhiều.";

        var results = products.Select(p =>
        {
            var finalPrice = p.Price * (100 - p.Discount) / 100;
            return $"- {p.ProductName} ({p.ViewCount} lượt xem): còn {finalPrice:N0} VNĐ";
        });

        return $"Sản phẩm được quan tâm nhiều:\n{string.Join("\n", results)}";
    }

    private async Task<string> GetLastOrderStatus(int userId)
    {
        var order = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefaultAsync();

        return order != null
            ? $"Đơn hàng gần nhất của bạn (mã #{order.OrderId}) đang ở trạng thái: {OrderStatusTranslator.Translate(order.OrderStatus)}."
            : "Bạn chưa có đơn hàng nào.";
    }
}

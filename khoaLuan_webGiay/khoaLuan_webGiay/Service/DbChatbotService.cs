using khoaLuan_webGiay.ChatKeyWords;
using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.Helpers;
using khoaLuan_webGiay.Service;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
            return "Bạn vui lòng nhập nội dung cần hỗ trợ.";

        userMessage = userMessage.ToLower().Trim();

        // 1. Phản hồi từ khóa tĩnh
        var keywordResponse = MatchKeyword(userMessage);
        if (!string.IsNullOrEmpty(keywordResponse))
            return keywordResponse;

        // 2. Truy vấn theo hãng giày
        var brandResponse = await HandleBrandQuery(userMessage);
        if (!string.IsNullOrEmpty(brandResponse))
            return brandResponse;

        // 3. Truy vấn giá sản phẩm
        var priceResponse = await HandlePriceQuery(userMessage);
        if (!string.IsNullOrEmpty(priceResponse))
            return priceResponse;

        // 4. Truy vấn đơn hàng người dùng
        if (userMessage.Contains("đơn hàng") && userId.HasValue)
        {
            var order = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();

            return order != null
                ? $"Đơn hàng gần nhất của bạn (mã #{order.OrderId}) đang ở trạng thái: {OrderStatusTranslator.Translate(order.OrderStatus)}."
                : "Bạn chưa có đơn hàng nào.";
        }

        // 5. Danh sách yêu thích
        if ((userMessage.Contains("yêu thích") || userMessage.Contains("đã lưu")) && userId.HasValue)
            return await ListFavoriteProducts(userId.Value);

        // 6. Danh sách hãng
        if (Regex.IsMatch(userMessage, @"(hãng|thương hiệu|loại giày)"))
            return await ListBrands();

        // 7. Đánh giá cao
        if (Regex.IsMatch(userMessage, @"(đánh giá cao|nhiều sao|tốt nhất)"))
            return await ListTopRatedProducts();

        // 8. Xem nhiều
        if (Regex.IsMatch(userMessage, @"(xem nhiều|phổ biến|quan tâm)"))
            return await ListMostViewedProducts();

        // 9. Hỏi về size sản phẩm
        if (userMessage.Contains("size") || userMessage.Contains("bao nhiêu size") || userMessage.Contains("cỡ giày"))
        {
            var productMatch = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.ProductName) && userMessage.Contains(p.ProductName.ToLower()))
                .Select(p => new { p.ProductId, p.ProductName })
                .FirstOrDefaultAsync();

            if (productMatch == null)
                return "Mình chưa xác định được sản phẩm bạn đang hỏi. Bạn có thể ghi rõ tên giày hơn được không?";

            var sizes = await _context.ProductSizes
                .Where(ps => ps.ProductId == productMatch.ProductId && ps.Quantity > 0)
                .Select(ps => new { ps.Size, ps.Quantity })
                .OrderBy(ps => ps.Size)
                .ToListAsync();

            if (!sizes.Any())
                return $"Hiện tại sản phẩm \"{productMatch.ProductName}\" đang hết size hoặc chưa cập nhật size.";

            var sizeInfo = string.Join(", ", sizes.Select(s => $"{s.Size} ({s.Quantity} đôi)"));
            return $"Sản phẩm \"{productMatch.ProductName}\" hiện có các size: {sizeInfo}.";
        }

        return "Xin lỗi, tôi chưa hiểu rõ yêu cầu của bạn. Bạn có thể hỏi lại hoặc cung cấp thêm thông tin nhé!";
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

    private async Task<string?> HandleBrandQuery(string msg)
    {
        var matchedCategory = await _context.Categories
            .Where(c => !string.IsNullOrEmpty(c.CategoryName) && msg.Contains(c.CategoryName.ToLower()))
            .Select(c => new { c.CategoryId, c.CategoryName })
            .FirstOrDefaultAsync();

        if (matchedCategory == null)
            return null;

        var products = await _context.Products
            .Where(p => p.CategoryId == matchedCategory.CategoryId)
            .OrderBy(p => p.Price)
            .ToListAsync();

        if (!products.Any())
            return $"Hiện chưa có sản phẩm nào thuộc hãng {matchedCategory.CategoryName}.";

        var cheapest = products.First();
        var mostExpensive = products.Last(); // đã được OrderBy(p => p.Price)

        return $"Hãng {matchedCategory.CategoryName} hiện có {products.Count} sản phẩm:\n" +
               $"- Rẻ nhất: \"{cheapest.ProductName}\" giá {cheapest.Price:N0} VNĐ\n" +
               $"- Đắt nhất: \"{mostExpensive.ProductName}\" giá {mostExpensive.Price:N0} VNĐ";

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


}

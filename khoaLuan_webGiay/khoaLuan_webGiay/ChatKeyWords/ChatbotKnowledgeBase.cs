namespace khoaLuan_webGiay.ChatKeyWords
{
    public static class ChatbotKnowledgeBase
    {
        public static readonly Dictionary<string[], string> KeywordResponses = new()
    {
        {
            new[] { "chào", "hi", "hello", "xin chào" },
            "Xin chào bạn 👋. Mình có thể hỗ trợ gì hôm nay?"
        },
        {
            new[] { "giảm giá", "khuyến mãi", "sale", "ưu đãi", "voucher" },
            "Hiện đang có chương trình Flash Sale đến 40%! Truy cập trang chủ để xem thêm."
        },
        {
            new[] { "thanh toán", "trả tiền", "cod" },
            "Bạn có thể thanh toán khi nhận hàng"
        },
        {
            new[] { "trả hàng", "đổi hàng", "bảo hành", "hỏng", "lỗi" },
            "Bạn có thể đổi/trả hàng trong vòng 14 ngày nếu chưa qua sử dụng và còn nguyên tem hộp."
        },
        {
            new[] { "giờ mở", "giờ làm", "mở cửa", "hoạt động", "đóng cửa" },
            "Cửa hàng mở cửa từ 8h đến 21h mỗi ngày, kể cả thứ 7, CN."
        },
        {
            new[] { "địa chỉ", "cửa hàng", "chi nhánh", "ở đâu" },
            "Chúng tôi hiện có cửa hàng tại Hẻm 114 Kiên Thị Nhãn, Phường 7, TP.Trà Vinh."
        },
        {
            new[] { "giúp", "bạn làm gì", "chatbot", "hỗ trợ" },
            "Mình có thể hỗ trợ bạn xem sản phẩm, hỏi giá, trạng thái đơn hàng, khuyến mãi và hơn thế nữa!"
        },
        {
            new[] { "vận chuyển", "giao hàng", "ship", "phí ship", "phí vận chuyển", "tiền ship" },
            "Chúng tôi hỗ trợ giao hàng toàn quốc, phí vận chuyển chỉ 50.000đ. Miễn phí với đơn trên 5 000.000đ!"
        },
        {
            new[] { "sản phẩm mới", "mới về", "mẫu mới", "hàng mới", "giày mới" },
            "Bạn có thể xem các mẫu mới nhất tại mục 'Sản phẩm mới' trên trang chủ nhé!"
        },
        {
            new[] { "size", "cỡ giày", "giày size", "chọn size", "kích thước" },
            "Giày hiện có size từ 39 đến 41. Nếu bạn cần tư vấn chọn size, mình sẽ hỗ trợ theo chiều dài chân!"
        },
        {
            new[] { "bền", "chất lượng", "chất liệu", "da thật", "vải", "giày có bền" },
            "Sản phẩm bên mình sử dụng chất liệu da tổng hợp cao cấp, siêu nhẹ, độ bền cao và bảo hành 6 tháng!"
        },
    };
    }
}

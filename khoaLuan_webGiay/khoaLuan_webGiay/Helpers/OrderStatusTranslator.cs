namespace khoaLuan_webGiay.Helpers
{
    public static class OrderStatusTranslator
    {
        public static string Translate(string status)
        {
            return status.ToLower() switch
            {
                "pending" => "Chờ xác nhận",
                "processing" => "Đang xử lý",
                "shipped" => "Đang giao",
                "delivered" => "Đã giao hàng",
                "cancelled" => "Đã hủy",
                "completed" => "Hoàn thành",
                "confirmed" => "Đã xác nhận",
                "returnRequested" => "Yêu cầu trả hàng",
                "returnConfirmed" => "Đã xác nhận trả hàng",
                _ => status
            };
        }
    }
}

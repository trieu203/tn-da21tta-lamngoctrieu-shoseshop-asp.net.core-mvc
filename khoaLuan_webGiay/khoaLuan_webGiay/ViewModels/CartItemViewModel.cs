namespace khoaLuan_webGiay.ViewModels
{
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtTime { get; set; }
        public string? ImageUrl { get; set; }
        public string? Size { get; set; }
        public decimal TotalPrice => Quantity * PriceAtTime;
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(i => i.TotalPrice);
        public string? Message { get; set; }
    }

    public class AddToCartViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public int Size { get; set; }
    }
}

namespace khoaLuan_webGiay.ViewModels
{
    public class CategoryVM
    {
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public List<ProductVM> Products { get; set; } = new();
        public PaginatedList<ProductVM> PaginatedProducts { get; set; } = default!;
    }
}

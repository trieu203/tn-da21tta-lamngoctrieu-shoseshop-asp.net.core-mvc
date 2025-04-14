using khoaLuan_webGiay.Data;

namespace khoaLuan_webGiay.ViewModels
{
    public class HomeVM
    {
        public PaginatedList<ProductVM> PaginatedProducts { get; set; }
        public List<ProductVM> NewProducts { get; set; } = new();
        public List<ProductVM> FeaturedProducts { get; set; } = new();
        public List<ProductVM> BestSellingProducts { get; set; } = new();
    }
}

using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace khoaLuan_webGiay.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly KhoaLuanContext _context;

        public HomeController(KhoaLuanContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? Category, int page = 1, int pageSize = 9)
        {
            var productQuery = _context.Products.AsQueryable();

            if (Category.HasValue)
            {
                productQuery = productQuery.Where(p => p.CategoryId == Category.Value);
            }

            int totalItems = await productQuery.CountAsync();

            var products = await productQuery
                .OrderByDescending(p => p.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ProjectToProductVM())
                .ToListAsync();

            var featuredProducts = await _context.Products
                .Where(p =>
                    (p.TotalSold >= 1 && p.AverageRating >= 4.0m) ||
                    (p.ViewCount >= 5 && p.AverageRating >= 3.5m) ||
                    (p.ViewCount >= 10 && p.TotalSold == 0 && p.AverageRating == 0))
                .OrderByDescending(p => p.AverageRating)
                .ThenByDescending(p => p.TotalSold)
                .ThenByDescending(p => p.ViewCount)
                .Take(8)
                .Select(ProjectToProductVM())
                .ToListAsync();

            var newProducts = await _context.Products
                .OrderByDescending(p => p.CreatedDate)
                .Take(8)
                .Select(ProjectToProductVM())
                .ToListAsync();

            var bestSellingProducts = await _context.Products
                .OrderByDescending(p => p.TotalSold)
                .Take(8)
                .Select(ProjectToProductVM())
                .ToListAsync();

            var paginatedResult = new PaginatedList<ProductVM>(products, totalItems, page, pageSize);

            var homeVM = new HomeVM
            {
                PaginatedProducts = paginatedResult,
                FeaturedProducts = featuredProducts,
                NewProducts = newProducts,
                BestSellingProducts = bestSellingProducts
            };

            ViewBag.CurrentCategory = Category;

            return View(homeVM);
        }

        private static Expression<Func<Product, ProductVM>> ProjectToProductVM()
        {
            return p => new ProductVM
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Price = p.Price,
                Discount = p.Discount,
                ImageUrl = p.ImageUrl,
                Rating = (int?)Math.Round(p.AverageRating),
                TotalSold = p.TotalSold,
                ViewCount = p.ViewCount
            };
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
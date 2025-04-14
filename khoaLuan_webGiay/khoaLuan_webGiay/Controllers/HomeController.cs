using System.Diagnostics;
using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.Models;
using khoaLuan_webGiay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khoaLuan_webGiay.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly KhoaLuanContext _context;

        public HomeController(KhoaLuanContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? Category, int page = 1, int pageSize = 9)
        {
            ViewBag.CategoryList = await _context.Categories.ToListAsync();
            var productQuery = _context.Products.AsQueryable();

            if (Category.HasValue)
            {
                productQuery = productQuery.Where(p => p.CategoryId == Category.Value);
            }

            int totalItems = await productQuery.CountAsync();

            var products = await productQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductVM
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Discount = p.Discount,
                    ImageUrl = p.ImageUrl,
                    Rating = p.AverageRating != null ? (int?)Math.Round(p.AverageRating) : null

                }).ToListAsync();

            // Sản phẩm mới
            var newProducts = await _context.Products
                .OrderByDescending(p => p.CreatedDate)
                .Take(8)
                .Select(p => new ProductVM
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Discount = p.Discount,
                    ImageUrl = p.ImageUrl,
                    Rating = p.AverageRating != null ? (int?)Math.Round(p.AverageRating) : null
                }).ToListAsync();

            // Sản phẩm nổi bật
            var featuredProducts = await _context.Products
                .Where(p => p.AverageRating >= 4.0m && p.Reviews.Count >= 5)
                .OrderByDescending(p => p.AverageRating)
                .Take(8)
                .Select(p => new ProductVM
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Discount = p.Discount,
                    ImageUrl = p.ImageUrl,
                    Rating = p.AverageRating != null ? (int?)Math.Round(p.AverageRating) : null
                }).ToListAsync();

            // Sản phẩm bán chạy
            var bestSellingProducts = await _context.Products
                .OrderByDescending(p => p.TotalSold)
                .Take(8)
                .Select(p => new ProductVM
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Discount = p.Discount,
                    ImageUrl = p.ImageUrl,
                    Rating = p.AverageRating != null ? (int?)Math.Round(p.AverageRating) : null
                }).ToListAsync();

            var viewModel = new HomeVM
            {
                PaginatedProducts = new PaginatedList<ProductVM>(products, totalItems, page, pageSize),
                NewProducts = newProducts,
                FeaturedProducts = featuredProducts,
                BestSellingProducts = bestSellingProducts
            };

            ViewBag.CurrentCategory = Category;
            return View(viewModel);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

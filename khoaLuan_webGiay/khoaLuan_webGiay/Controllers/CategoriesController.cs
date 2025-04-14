using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khoaLuan_webGiay.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly KhoaLuanContext _context;

        public CategoriesController(KhoaLuanContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.ToListAsync());
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id, int page = 1, int pageSize = 4)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null) return NotFound();

            var query = _context.Products
                .Where(p => p.CategoryId == id)
                .Select(p => new ProductVM
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Discount = p.Discount,
                    ImageUrl = p.ImageUrl,
                    Rating = (int)Math.Round(p.AverageRating)
                });

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new CategoryVM
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                PaginatedProducts = new PaginatedList<ProductVM>(items, totalItems, page, pageSize)
            };

            return View(viewModel);
        }


        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}

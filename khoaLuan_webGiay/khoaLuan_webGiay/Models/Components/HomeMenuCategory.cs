using khoaLuan_webGiay.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khoaLuan_webGiay.Models.Components
{
    public class HomeMenuCategory : ViewComponent
    {
        private readonly KhoaLuanContext _context;

        public HomeMenuCategory(KhoaLuanContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }
    }
}

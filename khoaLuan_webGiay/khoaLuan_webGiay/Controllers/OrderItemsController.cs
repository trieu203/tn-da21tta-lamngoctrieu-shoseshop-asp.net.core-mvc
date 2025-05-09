using khoaLuan_webGiay.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace khoaLuan_webGiay.Controllers
{
    public class OrderItemsController : Controller
    {
        private readonly KhoaLuanContext _context;

        public OrderItemsController(KhoaLuanContext context)
        {
            _context = context;
        }

        // GET: OrderItems
        public async Task<IActionResult> Index()
        {
            var khoaLuanContext = _context.OrderItems.Include(o => o.Order).Include(o => o.Product);
            return View(await khoaLuanContext.ToListAsync());
        }

        private bool OrderItemExists(int id)
        {
            return _context.OrderItems.Any(e => e.OrderItemId == id);
        }
    }
}

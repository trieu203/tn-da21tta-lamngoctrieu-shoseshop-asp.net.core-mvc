using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace khoaLuan_webGiay.Controllers
{
    public class OrdersController : Controller
    {
        private readonly KhoaLuanContext _context;

        public OrdersController(KhoaLuanContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var khoaLuanContext = _context.Orders.Include(o => o.User);
            return View(await khoaLuanContext.ToListAsync());
        }

        //Lịch sử mua hàng
        public IActionResult History()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Users");
            }

            // Lấy danh sách đơn hàng của người dùng
            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == (int?)int.Parse(userId))
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // Tạo ViewModel
            var model = orders.Select(o => new OrderHistoryViewModel
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate.HasValue ? o.OrderDate.Value.ToString("dd/MM/yyyy HH:mm") : "N/A",
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus ?? "N/A",
                OrderItems = o.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductId = oi.ProductId ?? 0,
                    ProductName = oi.Product?.ProductName ?? "Sản phẩm không tồn tại",
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    Size = oi.Size
                }).ToList()
            }).ToList();

            return View(model);
        }

        //Hủy đơn hàng
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Users");
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == int.Parse(userId));

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("History");
            }

            if (order.OrderStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy đơn hàng đang chờ xử lý.";
                return RedirectToAction("History");
            }

            // Hoàn trả tồn kho theo từng size
            foreach (var item in order.OrderItems)
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.Size == item.Size);

                if (productSize != null)
                {
                    productSize.Quantity += item.Quantity;
                    _context.ProductSizes.Update(productSize);
                }
            }

            order.OrderStatus = "Cancelled";
            _context.Orders.Update(order);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công và số lượng sản phẩm đã được hoàn trả.";
            return RedirectToAction("History");
        }

        //Xác nhận
        [HttpPost]
        public IActionResult ConfirmReceivedOrder(int orderId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Users");
            }

            // Tìm đơn hàng dựa trên orderId và userId
            var order = _context.Orders
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == int.Parse(userId));

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("History");
            }

            if (order.OrderStatus != "Shipped")
            {
                TempData["ErrorMessage"] = "Chỉ có thể xác nhận các đơn hàng đã được giao.";
                return RedirectToAction("History");
            }

            // Cập nhật trạng thái đơn hàng thành "Completed"
            order.OrderStatus = "Completed";
            _context.Orders.Update(order);

            // Lưu thay đổi vào cơ sở dữ liệu
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đơn hàng đã được xác nhận thành công.";
            return RedirectToAction("History");
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Nếu id = null, trả về lỗi 404
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}

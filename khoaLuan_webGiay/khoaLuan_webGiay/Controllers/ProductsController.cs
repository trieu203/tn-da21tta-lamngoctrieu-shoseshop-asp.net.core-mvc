using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace khoaLuan_webGiay.Controllers
{
    public class ProductsController : Controller
    {
        private readonly KhoaLuanContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(KhoaLuanContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? Category, int page = 1, int pageSize = 9)
        {
            _logger.LogInformation("Truy cập danh sách sản phẩm. Category: {Category}, Page: {Page}, PageSize: {PageSize}", Category, page, pageSize);

            try
            {
                var productQuery = _context.Products.AsQueryable();

                if (Category.HasValue)
                {
                    _logger.LogInformation("Lọc sản phẩm theo danh mục {Category}", Category.Value);
                    productQuery = productQuery.Where(p => p.CategoryId == Category.Value);
                }

                int totalItems = await productQuery.CountAsync();
                _logger.LogInformation("Tổng số sản phẩm: {TotalItems}", totalItems);

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
                        Rating = _context.Reviews
                                    .Where(r => r.ProductId == p.ProductId)
                                    .Select(r => r.Rating)
                                    .FirstOrDefault()
                    })
                    .ToListAsync();

                _logger.LogInformation("Lấy sản phẩm thành công. Số lượng: {ProductCount}", products.Count);

                var paginatedResult = new PaginatedList<ProductVM>(products, totalItems, page, pageSize);
                ViewBag.CurrentCategory = Category;

                return View(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm.");
                return StatusCode(500, "Đã xảy ra lỗi, vui lòng thử lại sau.");
            }
        }


        //Search
        public async Task<IActionResult> Search(string? query, int page = 1, int pageSize = 9)
        {
            // Khởi tạo truy vấn cơ bản
            var productQuery = _context.Products.AsQueryable();

            // Nếu có từ khóa tìm kiếm, áp dụng lọc theo tên sản phẩm
            if (!string.IsNullOrWhiteSpace(query))
            {
                productQuery = productQuery.Where(p => p.ProductName.Contains(query));
            }

            // Tính toán tổng số sản phẩm và phân trang
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
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync();

            // Tạo đối tượng PaginatedList chứa sản phẩm đã phân trang
            var paginatedResult = new PaginatedList<ProductVM>(products, totalItems, page, pageSize);

            // Truyền đối tượng PaginatedList vào View
            return View(paginatedResult);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Không tìm thấy ID sản phẩm.");
                return NotFound();
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductSizes)
                    .Include(p => p.Reviews)
                        .ThenInclude(r => r.User)
                    .Include(p => p.WishLists)
                    .Include(p => p.CartItems)
                    .SingleOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm với ID: {Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("Lấy chi tiết sản phẩm thành công. ID: {Id}", id);


                product.ViewCount++;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                ViewBag.CanReview = false;
                ViewBag.OrderItemId = null;
                if (User.Identity.IsAuthenticated)
                {
                    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var hasReviewable = await _context.Orders
                        .Include(o => o.OrderItems)
                        .Where(o => o.UserId == currentUserId && o.OrderStatus == "Completed")
                        .SelectMany(o => o.OrderItems)
                        .Where(oi => oi.ProductId == id && !oi.IsReviewed)
                        .FirstOrDefaultAsync();

                    if (hasReviewable != null)
                    {
                        ViewBag.CanReview = true;
                        ViewBag.OrderItemId = hasReviewable.OrderItemId;
                    }
                    else if (TempData["EnableReview"] != null && (bool)TempData["EnableReview"])
                    {
                        ViewBag.CanReview = true;
                        ViewBag.OrderItemId = TempData["OrderItemId"];
                    }
                }


                ViewBag.QuantityAvailable = product.ProductSizes.Sum(ps => ps.Quantity);
                var reviews = product.Reviews
                    .Where(r => r.ProductId == id)
                    .Select(r => new ReviewVM
                    {
                        Rating = r.Rating ?? 0,
                        Comment = r.Comment,
                        UserName = r.User?.UserName ?? "Ẩn danh",
                        Email = r.User?.Email ?? "Không có email",
                        ReviewDate = r.ReviewDate ?? DateTime.MinValue,
                        ImageUrl = r.User?.ImageUrl ?? "user_boy.jpg"
                    })
                    .ToList();

                var result = new ProductVMDT
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Description = product.Description,
                    Price = product.Price,
                    Discount = product.Discount,
                    ImageUrl = product.ImageUrl,
                    Reviews = reviews,
                    AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0
                };

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết sản phẩm với ID: {Id}", id);
                return StatusCode(500, "Đã xảy ra lỗi, vui lòng thử lại sau.");
            }
        }

        //Review
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment, int orderItemId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Users");

            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn số sao và nhập đánh giá.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var orderItem = await _context.OrderItems.FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId && !oi.IsReviewed);
            if (orderItem == null)
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = int.Parse(userId),
                Rating = rating,
                Comment = comment,
                ReviewDate = DateTime.Now
            };

            _context.Reviews.Add(review);
            orderItem.IsReviewed = true;
            _context.OrderItems.Update(orderItem);
            await _context.SaveChangesAsync();

            var product = await _context.Products.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product != null)
            {
                product.AverageRating = product.Reviews.Any()
                ? (decimal)product.Reviews.Average(r => r.Rating ?? 0)
                : 0;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        [Authorize]
        public IActionResult ReviewPurchasedProducts(int orderId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var order = _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId && o.OrderStatus == "Completed");

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng để đánh giá.";
                return RedirectToAction("History", "Orders");
            }

            var model = order.OrderItems
                .Where(oi => !oi.IsReviewed)
                .Select(oi => new ProductReviewInputVM
                {
                    ProductId = oi.ProductId ?? 0,
                    ProductName = oi.Product?.ProductName ?? "",
                    OrderItemId = oi.OrderItemId
                }).ToList();

            return View("ReviewPurchasedProducts", model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitProductReviews(List<ProductReviewInputVM> reviews)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            foreach (var review in reviews)
            {
                if (review.Rating >= 1 && review.Rating <= 5 && !string.IsNullOrWhiteSpace(review.Comment))
                {
                    var newReview = new Review
                    {
                        ProductId = review.ProductId,
                        UserId = userId,
                        Rating = review.Rating,
                        Comment = review.Comment,
                        ReviewDate = DateTime.Now
                    };

                    _context.Reviews.Add(newReview);

                    var orderItem = await _context.OrderItems.FindAsync(review.OrderItemId);
                    if (orderItem != null)
                    {
                        orderItem.IsReviewed = true;
                        _context.OrderItems.Update(orderItem);
                    }

                    var product = await _context.Products.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.ProductId == review.ProductId);
                    if (product != null)
                    {
                        product.AverageRating = product.Reviews.Any()
                        ? (decimal)product.Reviews.Average(r => r.Rating ?? 0)
                        : 0;
                        _context.Products.Update(product);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("History", "Orders");
        }
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}

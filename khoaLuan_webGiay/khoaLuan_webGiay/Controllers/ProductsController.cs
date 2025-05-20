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

        public async Task<IActionResult> Index(int? Category, string? priceRange, int page = 1, int pageSize = 9)
        {
            _logger.LogInformation("Truy cập danh sách sản phẩm. Category: {Category}, PriceRange: {PriceRange}, Page: {Page}, PageSize: {PageSize}", Category, priceRange, page, pageSize);

            try
            {
                var productQuery = _context.Products.AsQueryable();

                if (Category.HasValue)
                {
                    _logger.LogInformation("Lọc sản phẩm theo danh mục {Category}", Category.Value);
                    productQuery = productQuery.Where(p => p.CategoryId == Category.Value);
                }

                // Lọc theo giá
                if (!string.IsNullOrEmpty(priceRange))
                {
                    var parts = priceRange.Split("-");
                    if (parts.Length == 2 &&
                        decimal.TryParse(parts[0], out decimal minPrice) &&
                        decimal.TryParse(parts[1], out decimal maxPrice))
                    {
                        _logger.LogInformation("Lọc theo khoảng giá: {Min} - {Max}", minPrice, maxPrice);
                        productQuery = productQuery.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
                    }
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
                        Rating = _context.Reviews
                                    .Where(r => r.ProductId == p.ProductId)
                                    .Select(r => r.Rating)
                                    .FirstOrDefault()
                    })
                    .ToListAsync();

                var paginatedResult = new PaginatedList<ProductVM>(products, totalItems, page, pageSize);

                ViewBag.CurrentCategory = Category;
                ViewBag.CurrentPriceRange = priceRange;

                return View(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm.");
                return StatusCode(500, "Đã xảy ra lỗi, vui lòng thử lại sau.");
            }
        }

        //Search
        public async Task<IActionResult> Search(string? query, string? priceRange, int page = 1, int pageSize = 9)
        {
            // Khởi tạo truy vấn cơ bản
            var productQuery = _context.Products.AsQueryable();

            // Nếu có từ khóa tìm kiếm, áp dụng lọc theo tên sản phẩm
            if (!string.IsNullOrWhiteSpace(query))
            {
                productQuery = productQuery.Where(p => p.ProductName.Contains(query));
            }

            // Lọc theo giá
            if (!string.IsNullOrEmpty(priceRange))
            {
                var parts = priceRange.Split("-");
                if (parts.Length == 2 &&
                    decimal.TryParse(parts[0], out decimal minPrice) &&
                    decimal.TryParse(parts[1], out decimal maxPrice))
                {
                    _logger.LogInformation("Lọc theo khoảng giá: {Min} - {Max}", minPrice, maxPrice);
                    productQuery = productQuery.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
                }
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

                ViewBag.SizeQuantities = product.ProductSizes
                .GroupBy(ps => ps.Size)
                .Select(g => new { Size = g.Key, Quantity = g.Sum(p => p.Quantity) })
                .ToDictionary(x => x.Size, x => x.Quantity);


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

                // Lấy sản phẩm tương tự: cùng danh mục, loại trừ chính nó
                var relatedProducts = await _context.Products
                    .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                    .OrderByDescending(p => p.ViewCount) // hoặc theo Rating
                    .Take(6)
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

                ViewBag.RelatedProducts = relatedProducts;

                ViewBag.QuantityAvailable = product.ProductSizes.Sum(ps => ps.Quantity);
                var reviews = product.Reviews
                    .Where(r => r.ProductId == id)
                    .Select(r => new ReviewVM
                    {
                        Rating = r.Rating ?? 0,
                        Comment = r.Comment,
                        FullName = r.User?.FullName ?? "Ẩn danh",
                        Email = r.User?.Email ?? "Không có email",
                        ReviewDate = r.ReviewDate ?? DateTime.MinValue,
                        ImageUrl = r.User?.ImageUrl ?? "user_boy.jpg",
                        MediaUrls = r.MediaUrls
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
        [RequestSizeLimit(104857600)]
        [Authorize]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment, int orderItemId, List<IFormFile> MediaFiles)
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

            // Xử lý file upload
            if (MediaFiles != null && MediaFiles.Count > 0)
            {
                var paths = new List<string>();
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reviews");
                Directory.CreateDirectory(uploadFolder);

                string[] allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };

                foreach (var file in MediaFiles)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName);
                        if (!allowedExtensions.Contains(ext)) continue;

                        var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                        if (baseName.Length > 6)
                            baseName = baseName.Substring(0, 6);

                        // Gắn thêm một phần định danh ngắn gọn để tránh trùng (3 ký tự random)
                        var shortId = Guid.NewGuid().ToString("N").Substring(0, 3);

                        var uniqueName = $"{baseName}_{shortId}{ext}";
                        var savePath = Path.Combine(uploadFolder, uniqueName);

                        using (var stream = new FileStream(savePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        paths.Add($"/uploads/reviews/{uniqueName}");
                    }
                }

                review.MediaUrls = string.Join(";", paths);
            }


            _context.Reviews.Add(review);
            orderItem.IsReviewed = true;
            _context.OrderItems.Update(orderItem);
            await _context.SaveChangesAsync();

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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId) || userId == 0)
            {
                return Unauthorized();
            }

            foreach (var reviewVm in reviews)
            {
                if (reviewVm.Rating < 1 || reviewVm.Rating > 5 || string.IsNullOrWhiteSpace(reviewVm.Comment))
                    continue;

                var newReview = new Review
                {
                    ProductId = reviewVm.ProductId,
                    UserId = userId,
                    Rating = reviewVm.Rating,
                    Comment = reviewVm.Comment.Trim(),
                    ReviewDate = DateTime.UtcNow
                };

                if (reviewVm.MediaFiles != null && reviewVm.MediaFiles.Any())
                {
                    var uploadPaths = new List<string>();
                    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reviews");
                    Directory.CreateDirectory(uploadFolder);

                    foreach (var file in reviewVm.MediaFiles)
                    {
                        if (file.Length > 0)
                        {
                            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                            var extension = Path.GetExtension(file.FileName);

                            if (originalName.Length > 6)
                                originalName = originalName.Substring(0, 6);

                            var uniqueName = $"{Guid.NewGuid()}_{originalName}{extension}";
                            var path = Path.Combine(uploadFolder, uniqueName);

                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            uploadPaths.Add("/uploads/reviews/" + uniqueName);
                        }
                    }

                    newReview.MediaUrls = string.Join(";", uploadPaths);
                }

                _context.Reviews.Add(newReview);

                var orderItem = await _context.OrderItems.FindAsync(reviewVm.OrderItemId);
                if (orderItem != null && !orderItem.IsReviewed)
                {
                    orderItem.IsReviewed = true;
                    _context.OrderItems.Update(orderItem);
                }

                var product = await _context.Products.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.ProductId == reviewVm.ProductId);
                if (product != null)
                {
                    var ratings = product.Reviews.Select(r => r.Rating ?? 0).ToList();
                    ratings.Add(reviewVm.Rating);
                    product.AverageRating = (decimal)ratings.Average();
                    _context.Products.Update(product);
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

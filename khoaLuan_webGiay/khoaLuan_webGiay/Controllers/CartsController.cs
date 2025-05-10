using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace khoaLuan_webGiay.Controllers
{
    public class CartsController : Controller
    {
        private readonly KhoaLuanContext _context;

        public CartsController(KhoaLuanContext context)
        {
            _context = context;
        }

        // GET: Carts
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Users");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == int.Parse(userId) && c.IsActive);

            var model = new CartViewModel();

            if (cart == null || !cart.CartItems.Any())
            {
                model.Message = "Giỏ hàng của bạn đang trống.";
                return View(model);
            }

            model.Items = cart.CartItems.Select(ci => new CartItemViewModel
            {
                CartItemId = ci.CartItemId,
                ProductId = ci.ProductId.Value,
                ProductName = ci.ProductName,
                Quantity = ci.Quantity,
                PriceAtTime = ci.PriceAtTime,
                ImageUrl = ci.ImageUrl,
                Size = ci.Size
            }).ToList();

            return View(model);
        }

        //Thêm sản phẩm vào giỏ
        [HttpPost]
        public async Task<IActionResult> AddToCart(AddToCartViewModel model)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Users");

            var userIdInt = int.Parse(userId);

            if (model.ProductId <= 0)
            {
                TempData["Message"] = "Sản phẩm không hợp lệ.";
                return RedirectToAction("Index", "Products");
            }

            if (model.Size == 0)
            {
                TempData["Message"] = "Vui lòng chọn kích thước sản phẩm.";
                TempData["SelectedSize"] = model.Size;
                TempData["SelectedQuantity"] = model.Quantity;
                return RedirectToAction("Details", "Products", new { id = model.ProductId });
            }

            if (model.Quantity <= 0)
            {
                TempData["Message"] = "Số lượng không hợp lệ.";
                TempData["SelectedSize"] = model.Size;
                TempData["SelectedQuantity"] = model.Quantity;
                return RedirectToAction("Details", "Products", new { id = model.ProductId });
            }

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);

            var productSize = await _context.ProductSizes
                .Include(ps => ps.Product)
                .FirstOrDefaultAsync(ps => ps.ProductId == model.ProductId && ps.Size == model.Size.ToString());

            if (productSize == null)
            {
                TempData["Message"] = "Kích thước sản phẩm không hợp lệ.";
                return RedirectToAction("Details", "Products", new { id = model.ProductId });
            }

            // Lấy giỏ hàng hiện tại
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userIdInt && c.IsActive);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userIdInt,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync(); // để có CartId
            }

            // Số lượng đã giữ trong tất cả giỏ (gồm user này)
            var reservedQty = await _context.CartItems
                .Include(ci => ci.Cart)
                .Where(ci => ci.ProductId == model.ProductId
                          && ci.Size == model.Size.ToString()
                          && ci.Cart.IsActive)
                .SumAsync(ci => ci.Quantity);

            var availableQty = productSize.Quantity - reservedQty;

            // Kiểm tra sản phẩm đã có trong giỏ chưa
            var existingItem = cart.CartItems
                .FirstOrDefault(ci => ci.ProductId == model.ProductId && ci.Size == model.Size.ToString());

            var newTotalQty = (existingItem?.Quantity ?? 0) + model.Quantity;

            if (newTotalQty > productSize.Quantity)
            {
                var canAddMore = productSize.Quantity - (existingItem?.Quantity ?? 0);
                TempData["Message"] = $"Chỉ còn {canAddMore} sản phẩm có thể thêm vào giỏ.";
                return RedirectToAction("Details", "Products", new { id = model.ProductId });
            }

            if (existingItem != null)
            {
                existingItem.Quantity += model.Quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productSize.ProductId,
                    Quantity = model.Quantity,
                    ProductName = productSize.Product.ProductName,
                    PriceAtTime = productSize.Product.Price,
                    Size = model.Size.ToString(),
                    ImageUrl = productSize.Product.ImageUrl
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Message"] = "Sản phẩm đã được thêm vào giỏ hàng.";
            return RedirectToAction("Index");
        }



        // Kiểm tra số lượng sản phẩm
        private async Task<int> GetReservedQuantity(int productId, string size, int excludingUserId)
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)
                .Where(ci => ci.ProductId == productId
                    && ci.Size == size
                    && ci.Cart.IsActive
                    && ci.Cart.UserId != excludingUserId)
                .SumAsync(ci => ci.Quantity);
        }


        // Removw Cart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Users");

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == id && ci.Cart.UserId == int.Parse(userId) && ci.Cart.IsActive);

            if (cartItem != null)
            {
                var cart = cartItem.Cart;

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                // Kiểm tra nếu giỏ hàng rỗng sau khi xóa
                var remainingItems = await _context.CartItems
                    .Where(ci => ci.CartId == cart.CartId)
                    .CountAsync();

                if (remainingItems == 0)
                {
                    cart.IsActive = false; 
                    _context.Carts.Update(cart);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            if (quantity <= 0)
                return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });

            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Bạn cần đăng nhập." });

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.UserId == int.Parse(userId) && ci.Cart.IsActive);

            if (cartItem == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

            var productSize = await _context.ProductSizes
                .FirstOrDefaultAsync(ps => ps.ProductId == cartItem.ProductId && ps.Size == cartItem.Size);

            if (productSize == null || quantity > productSize.Quantity)
            {
                var available = productSize?.Quantity ?? 0;
                return Json(new { success = false, message = $"Chỉ còn {available} sản phẩm." });
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            var totalPrice = cartItem.Quantity * cartItem.PriceAtTime;
            var cartTotal = await _context.CartItems
                .Where(ci => ci.Cart.UserId == int.Parse(userId) && ci.Cart.IsActive)
                .SumAsync(ci => ci.Quantity * ci.PriceAtTime);

            return Json(new { success = true, totalPrice, cartTotal });
        }

        //Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userIdString = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Users");
            }

            int userId = int.Parse(userIdString);

            // Lấy thông tin người dùng
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Index", "Carts");
            }

            // Lấy giỏ hàng
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Carts");
            }

            var model = new CheckoutViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ShippingAddress = user.Address, // Giả sử bảng Users có trường Address
                CartItems = cart.CartItems.Select(ci => new CartItemViewModel
                {
                    ProductName = ci.Product.ProductName,
                    Quantity = ci.Quantity,
                    PriceAtTime = ci.PriceAtTime,
                    Discount = ci.Product.Discount
                }).ToList()
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userIdString = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Users");

            int userId = int.Parse(userIdString);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Index", "Carts");
            }

            if (!ModelState.IsValid)
            {
                var cartReload = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

                if (cartReload != null)
                {
                    model.CartItems = cartReload.CartItems
                        .Select(ci => new CartItemViewModel
                        {
                            ProductName = ci.Product.ProductName,
                            Quantity = ci.Quantity,
                            PriceAtTime = ci.PriceAtTime,
                            Discount = ci.Product.Discount
                        }).ToList();
                }

                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Carts");
            }

            // ✅ Kiểm tra tồn kho trước khi tạo Order
            foreach (var item in cart.CartItems)
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.Size == item.Size);

                if (productSize == null)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Không tìm thấy kho cho sản phẩm {item.Product.ProductName} size {item.Size}.";
                    return RedirectToAction("Index", "Carts");
                }

                if (productSize.Quantity < item.Quantity)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Sản phẩm {item.Product.ProductName} size {item.Size} chỉ còn {productSize.Quantity} sản phẩm trong kho.";
                    return RedirectToAction("Index", "Carts");
                }
            }

            var subtotal = cart.CartItems.Sum(ci => ci.PriceAtTime * ci.Quantity);
            var totalDiscount = cart.CartItems.Sum(ci =>
            {
                var discount = ci.Product?.Discount ?? 0;
                return (ci.PriceAtTime * ci.Quantity * discount) / 100;
            });
            var shippingFee = 50000;
            var total = subtotal - totalDiscount + shippingFee;

            var fullName = Request.Form["NewFullName"].FirstOrDefault();
            var phoneNumber = Request.Form["NewPhoneNumber"].FirstOrDefault();
            var shippingAddress = Request.Form["NewShippingAddress"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(fullName)) fullName = model.FullName;
            if (string.IsNullOrWhiteSpace(phoneNumber)) phoneNumber = model.PhoneNumber;
            if (string.IsNullOrWhiteSpace(shippingAddress)) shippingAddress = model.ShippingAddress;

            // ✅ Tạo đơn hàng **sau khi đã kiểm tra tồn kho**
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = total,
                OrderStatus = "Pending",
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Email = model.Email,
                ShippingAddress = shippingAddress
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart.CartItems)
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.Size == item.Size);

                // Đã được kiểm tra trước đó
                productSize.Quantity -= item.Quantity;
                _context.ProductSizes.Update(productSize);

                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.PriceAtTime,
                    Size = item.Size
                };
                _context.OrderItems.Add(orderItem);
            }

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.IsActive = false;
            _context.Carts.Update(cart);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Đơn hàng của bạn đã được tạo thành công.";
            return RedirectToAction("Index", "Orders");
        }

        private bool CartExists(int id)
        {
            return _context.Carts.Any(e => e.CartId == id);
        }
    }
}

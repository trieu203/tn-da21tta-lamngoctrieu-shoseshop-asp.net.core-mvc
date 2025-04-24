using khoaLuan_webGiay.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khoaLuan_webGiay.Controllers
{
    public class WishListsController : Controller
    {
        private readonly KhoaLuanContext _context;

        public WishListsController(KhoaLuanContext context)
        {
            _context = context;
        }

        // GET: WishLists
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                // Nếu người dùng chưa đăng nhập
                return RedirectToAction("Login", "Users");
            }

            // Lấy danh sách yêu thích của người dùng hiện tại
            var wishListItems = await _context.WishLists
                .Where(w => w.UserId.ToString() == userId)
                .Include(w => w.Product)
                .ToListAsync();

            return View(wishListItems);
        }

        //AddToWishList
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToWishList(int productId)
        {
            // Lấy ID người dùng từ Claims trong Cookie Authentication
            var userId = User.FindFirst("UserId")?.Value;

            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Users");
            }

            // Kiểm tra xem sản phẩm đã có trong danh sách yêu thích chưa
            var existingWishList = await _context.WishLists
                .FirstOrDefaultAsync(w => w.UserId == int.Parse(userId) && w.ProductId == productId);

            // Nếu sản phẩm đã có trong danh sách yêu thích, thông báo lỗi
            if (existingWishList != null)
            {
                TempData["ErrorMessage"] = "Sản phẩm đã có trong danh sách yêu thích của bạn.";
                return RedirectToAction("Index", "WishLists", new { id = productId });
            }

            // Nếu sản phẩm chưa có trong danh sách yêu thích, thêm vào
            var wishList = new WishList
            {
                UserId = int.Parse(userId),
                ProductId = productId,
                AddedDate = DateTime.Now
            };

            // Thêm sản phẩm vào danh sách yêu thích trong cơ sở dữ liệu
            _context.WishLists.Add(wishList);
            await _context.SaveChangesAsync(); // Lưu thay đổi vào cơ sở dữ liệu

            // Hiển thị thông báo thành công
            TempData["SuccessMessage"] = "Sản phẩm đã được thêm vào danh sách yêu thích!";
            return Redirect(Request.Headers["Referer"].ToString());
        }


        //DeleteWishList
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            // Kiểm tra nếu người dùng đã đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("login", "Users");
            }

            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Users");
            }

            // Tìm sản phẩm trong danh sách yêu thích của người dùng
            var wishListItem = await _context.WishLists
                .FirstOrDefaultAsync(w => w.UserId == int.Parse(userId) && w.ProductId == productId);

            if (wishListItem != null)
            {
                _context.WishLists.Remove(wishListItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sản phẩm đã được xóa khỏi danh sách yêu thích.";
            }
            else
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại trong danh sách yêu thích.";
            }

            return RedirectToAction("Index");
        }

        // GET: WishLists/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wishList = await _context.WishLists
                .Include(w => w.Product)
                .Include(w => w.User)
                .FirstOrDefaultAsync(m => m.WishListId == id);
            if (wishList == null)
            {
                return NotFound();
            }

            return View(wishList);
        }

        private bool WishListExists(int id)
        {
            return _context.WishLists.Any(e => e.WishListId == id);
        }
    }
}

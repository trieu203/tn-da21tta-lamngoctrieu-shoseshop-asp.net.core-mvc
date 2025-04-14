using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace khoaLuan_webGiay.Models.Components
{
    public class WishListViewComponent : ViewComponent
    {
        private readonly KhoaLuanContext _context;

        public WishListViewComponent(KhoaLuanContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            // Lấy User ID từ Claims
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                // Nếu người dùng chưa đăng nhập, trả về wishlist trống
                return View("WishListPanel", new WishListModel
                {
                    Quantity = 0
                });
            }

            // Lấy wishlist từ cơ sở dữ liệu
            var wishListItems = _context.WishLists
                .Include(w => w.Product) // Bao gồm thông tin sản phẩm
                .Where(w => w.UserId == int.Parse(userId))
                .ToList();

            if (wishListItems == null || !wishListItems.Any())
            {
                // Nếu không có sản phẩm trong wishlist
                return View("WishListPanel", new WishListModel
                {
                    Quantity = 0
                });
            }

            // Tổng hợp thông tin wishlist
            var wishListModel = new WishListModel
            {
                Quantity = wishListItems.Count
            };

            return View("WishListPanel", wishListModel);
        }
    }
}

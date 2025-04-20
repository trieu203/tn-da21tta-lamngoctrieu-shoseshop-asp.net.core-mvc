using khoaLuan_webGiay.Data;
using Microsoft.AspNetCore.Mvc;

namespace khoaLuan_webGiay.Models.Components
{
    public class UserAvatarViewComponent : ViewComponent
    {
        private readonly KhoaLuanContext _context;

        public UserAvatarViewComponent(KhoaLuanContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            string imageUrl = "user_boy.jpg"; // mặc định

            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = HttpContext.User.FindFirst("UserId")?.Value;

                if (int.TryParse(userIdClaim, out int userId))
                {
                    var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
                    if (user != null && !string.IsNullOrEmpty(user.ImageUrl))
                    {
                        imageUrl = user.ImageUrl;
                    }
                }
            }

            return View("Default", imageUrl);
        }
    }
}

using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.Service;
using khoaLuan_webGiay.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace khoaLuan_webGiay.Controllers
{
    public class UsersController : Controller
    {
        private readonly KhoaLuanContext _context;
        private readonly IEmailService _emailService;

        public UsersController(KhoaLuanContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }


        //Gửi OTP
        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] EmailRequest data)
        {
            var email = data.Email;

            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email không hợp lệ");

            var otp = new Random().Next(100000, 999999).ToString();

            var emailBody = $@"
                <p>Xin chào,</p>
                <p>Mã xác nhận OTP của bạn là: <strong style='color:blue;font-size:18px'>{otp}</strong></p>
                <p>Vui lòng không chia sẻ mã này với bất kỳ ai. Mã sẽ hết hạn sau 10 phút.</p>
                <p>Trân trọng,<br><b>Milion Sneaker</b></p>";

            await _emailService.SendAsync(email, "Mã xác nhận đăng ký", $"Mã OTP của bạn là: {otp}");

            var existing = _context.OtpConfirmations
                .Where(o => o.Email == email && !o.IsUsed)
                .ToList();

            _context.OtpConfirmations.RemoveRange(existing);

            var otpEntry = new OtpConfirmations
            {
                Email = email,
                OtpCode = otp,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            _context.Add(otpEntry);
            await _context.SaveChangesAsync();

            return Ok("Đã gửi mã OTP về email");
        }

        //Xác thực OTP
        [HttpPost]
        [HttpPost]
        public IActionResult VerifyOtp([FromBody] OtpVerifyRequest data)
        {
            string email = data.Email;
            string otp = data.Otp;

            var record = _context.OtpConfirmations
                .Where(o => o.Email == email && o.OtpCode == otp && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            if (record == null || (DateTime.UtcNow - record.CreatedAt).TotalMinutes > 10)
            {
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");
            }

            //record.IsUsed = true;
            //_context.SaveChanges();

            //TempData["VerifiedEmail"] = email;
            return Ok("Xác thực thành công");
        }

        //Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromForm] RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest("Thông tin không hợp lệ:\n" + string.Join("; ", errors));
            }


            // Kiểm tra email hoặc username đã tồn tại
            if (_context.Users.Any(u => u.Email == model.Email))
            {
                return BadRequest("Email đã được sử dụng");
            }

            if (_context.Users.Any(u => u.UserName == model.UserName))
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại");
                return View("Register", model);
            }

            // Map từ VM sang entity
            var user = new User
            {
                UserName = model.UserName,
                Password = model.Password,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Role = model.Role ?? "User",
                CreatedDate = DateOnly.FromDateTime(DateTime.Now)
            };

            // upload image
            if (model.Image != null && model.Image.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.Image.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/users", fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await model.Image.CopyToAsync(stream);
                user.ImageUrl = fileName;
            }

            _context.Users.Add(user);
            var result = await _context.SaveChangesAsync();
            Console.WriteLine("SaveChangesAsync result: " + result);

            return RedirectToAction("Login", "Users");
        }


        //Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u =>
                    (u.UserName == model.UserNameOrEmail || u.Email == model.UserNameOrEmail)
                    && u.Password == model.Password);

                if (user != null)
                {
                    // Tạo danh sách claims
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User"),
                new Claim("ImageUrl", user.ImageUrl ?? "user_boy.jpg")
            };

                    // Tạo identity và principal
                    var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                    var principal = new ClaimsPrincipal(identity);

                    // Đăng nhập bằng cookie
                    await HttpContext.SignInAsync("MyCookieAuth", principal);

                    if (!string.IsNullOrEmpty(user.Role) && user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Admin");
                    }

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu");

            }

            return View(model);
        }

        //LogOut
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login", "Users");
        }

        //ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                var allErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine("Validation Errors: " + string.Join("; ", allErrors));
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Không tìm thấy email này.");
                return View(model);
            }

            var otp = new Random().Next(100000, 999999).ToString();

            var existing = _context.OtpConfirmations.Where(x => x.Email == model.Email && !x.IsUsed);
            _context.OtpConfirmations.RemoveRange(existing);

            _context.Add(new OtpConfirmations
            {
                Email = model.Email,
                OtpCode = otp,
                CreatedAt = DateTime.Now,
                IsUsed = false
            });
            await _context.SaveChangesAsync();

            await _emailService.SendAsync(model.Email, "Khôi phục mật khẩu", $"Mã OTP của bạn là: {otp}");

            TempData["Email"] = model.Email;
            return RedirectToAction("ResetPassword");
        }

        //ResetPassword
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData["Email"] as string;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");

            var model = new ResetPasswordVM { Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Thông tin không hợp lệ.");

            var record = _context.OtpConfirmations
                .Where(o => o.Email == model.Email && o.OtpCode == model.Otp && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            if (record == null || (DateTime.UtcNow - record.CreatedAt).TotalMinutes > 15)
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
                return BadRequest("Không tìm thấy người dùng.");

            user.Password = model.NewPassword;
            record.IsUsed = true;

            _context.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Đặt lại mật khẩu thành công.");
        }

        [HttpPost]
        public async Task<IActionResult> SendOtpForReset([FromBody] EmailRequest data)
        {
            var email = data.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email)) return BadRequest("Email không hợp lệ.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return BadRequest("Không tìm thấy địa chỉ email. Hyax nhập địa chỉ email tài khoản của bạn ");

            var otp = new Random().Next(100000, 999999).ToString();

            var body = $"Mã OTP khôi phục mật khẩu là: {otp}";
            await _emailService.SendAsync(email, "Mã khôi phục mật khẩu", body);

            var existing = _context.OtpConfirmations.Where(x => x.Email == email && !x.IsUsed);
            _context.OtpConfirmations.RemoveRange(existing);

            _context.Add(new OtpConfirmations
            {
                Email = email,
                OtpCode = otp,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            });
            await _context.SaveChangesAsync();

            return Ok("Mã OTP đã gửi.");
        }

        //Detals User
        public async Task<IActionResult> Details()
        {
            // Lấy UserName từ User.Identity.Name
            string userName = User.Identity.Name;

            if (string.IsNullOrEmpty(userName))
            {
                // Nếu không tìm thấy tên người dùng, trả về lỗi 404
                return NotFound();
            }

            // Truy vấn User từ cơ sở dữ liệu dựa trên UserName
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userName);  // Giả sử UserName là tên người dùng

            if (user == null)
            {
                // Nếu không tìm thấy người dùng, trả về lỗi 404
                return NotFound();
            }

            // Tạo ViewModel để hiển thị thông tin người dùng
            var userViewModel = new UserVM
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                ImageUrl = user.ImageUrl,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role
            };

            // Trả về View với dữ liệu của người dùng
            return View(userViewModel);
        }

        //Edit User
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userEditVM = new UserEditVM
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                ImageUrl = user.ImageUrl,
                Role = user.Role
            };

            return View(userEditVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserEditVM model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (model.DateOfBirth.HasValue)
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var birthDate = model.DateOfBirth.Value;
                    var age = today.Year - birthDate.Year;
                    if (birthDate > today.AddYears(-age)) age--;

                    if (age < 15 || age > 100)
                    {
                        ModelState.AddModelError(string.Empty, "Tuổi phải nằm trong khoảng từ 15 đến 100.");
                        return View(model);
                    }
                }

                var existingUserWithSameEmailOrUsername = await _context.Users
                    .FirstOrDefaultAsync(u => (u.UserName == model.UserName || u.Email == model.Email) && u.UserId != model.UserId);
                if (existingUserWithSameEmailOrUsername != null)
                {
                    ModelState.AddModelError(string.Empty, "Tên người dùng hoặc email đã tồn tại.");
                    return View(model);
                }

                var emailRegex = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
                if (!Regex.IsMatch(model.Email, emailRegex))
                {
                    ModelState.AddModelError("Email", "Địa chỉ email không hợp lệ.");
                    return View(model);
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                string imageFileName = user.ImageUrl;

                if (model.Image != null)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(user.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "users", user.ImageUrl);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Tạo tên file mới: 6 ký tự ngẫu nhiên + đuôi mở rộng
                    var fileExtension = Path.GetExtension(model.Image.FileName);
                    imageFileName = Guid.NewGuid().ToString("N").Substring(0, 6) + fileExtension;

                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "users", imageFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(stream);
                    }
                }

                try
                {
                    // Cập nhật dữ liệu người dùng
                    user.UserName = model.UserName;
                    user.Email = model.Email;
                    user.FullName = model.FullName;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Address = model.Address;
                    user.Gender = model.Gender;
                    user.DateOfBirth = model.DateOfBirth;
                    user.ImageUrl = imageFileName;
                    user.Role = model.Role ?? "User";

                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        user.Password = model.Password;
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    // Cập nhật Claims (để ảnh mới hiển thị ngay)
                    await HttpContext.SignOutAsync();

                    var claims = new List<Claim>{
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim("ImageUrl", user.ImageUrl ?? "user_boy.jpg"),
                        new Claim(ClaimTypes.Role, user.Role ?? "User"),
                        new Claim("UserId", user.UserId.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    await HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(model.UserId))
                        return NotFound();
                    else
                        throw;
                }

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction(nameof(Details));
            }

            return View(model);
        }

        //Users Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            // Lấy UserId từ Claims
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản.");
            }

            // Xóa ảnh đại diện nếu có
            if (!string.IsNullOrEmpty(user.ImageUrl))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "users", user.ImageUrl);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Đăng xuất sau khi xóa
            await HttpContext.SignOutAsync("MyCookieAuth");

            TempData["SuccessMessage"] = "Tài khoản đã được xóa vĩnh viễn.";
            return RedirectToAction("Index", "Home");
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}

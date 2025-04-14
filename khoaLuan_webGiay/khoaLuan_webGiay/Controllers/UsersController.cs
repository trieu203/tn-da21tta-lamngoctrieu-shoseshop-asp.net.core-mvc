using khoaLuan_webGiay.Data;
using khoaLuan_webGiay.Service;
using khoaLuan_webGiay.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
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
                CreatedAt = DateTime.Now,
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

            if (record == null || (DateTime.Now - record.CreatedAt).TotalMinutes > 10)
            {
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");
            }

            record.IsUsed = true;
            _context.SaveChanges();

            TempData["VerifiedEmail"] = email;
            return Ok("Xác thực thành công");
        }

        //Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
                return View("RegisterForm", user);
            }

            user.CreatedDate = DateOnly.FromDateTime(DateTime.Now);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home"); // hoặc login
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
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

                    // Tạo identity và principal
                    var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                    var principal = new ClaimsPrincipal(identity);

                    // Đăng nhập bằng cookie
                    await HttpContext.SignInAsync("MyCookieAuth", principal);

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

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,UserName,Password,Email,FullName,Role,CreatedDate,PhoneNumber,Address,ImageUrl,Gender,DateOfBirth")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
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
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,UserName,Password,Email,FullName,Role,CreatedDate,PhoneNumber,Address,ImageUrl,Gender,DateOfBirth")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
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

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}

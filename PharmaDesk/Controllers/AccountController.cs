using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Data;
using PharmaDesk.Models;
using PharmaDesk.ViewModels;
using System.Security.Claims;

namespace PharmaDesk.Controllers
{
    public class AccountController : Controller
    {
        private readonly PharmaDeskContext _context;

        public AccountController(PharmaDeskContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public async Task<IActionResult> Login()
        {
            // جلب الموظفين النشطين فقط لعرضهم في القائمة
            var vm = new LoginViewModel
            {
                Users = await _context.TeamMembers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.FullName)
                    .ToListAsync()
            };
            return View(vm);
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Users = await _context.TeamMembers.Where(t => t.IsActive).ToListAsync();
                return View(vm);
            }

            var user = await _context.TeamMembers.FindAsync(vm.TeamMemberId);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "User not found or inactive.");
                vm.Users = await _context.TeamMembers.Where(t => t.IsActive).ToListAsync();
                return View(vm);
            }

            // 1. التحقق من القفل (5 دقائق)
            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.Now)
            {
                var remaining = (user.LockedUntil.Value - DateTime.Now).Minutes;
                ModelState.AddModelError("", $"Account is locked. Try again in {remaining} minutes.");
                vm.Users = await _context.TeamMembers.Where(t => t.IsActive).ToListAsync();
                return View(vm);
            }

            // 2. التحقق من الـ PIN
            var hasher = new PasswordHasher<TeamMember>();
            var result = hasher.VerifyHashedPassword(user, user.PinHash, vm.Pin);

            if (result == PasswordVerificationResult.Failed)
            {
                // زيادة عدد المحاولات الفاشلة
                user.FailedPinCount++;
                if (user.FailedPinCount >= 5)
                {
                    user.LockedUntil = DateTime.Now.AddMinutes(5);
                    user.FailedPinCount = 0; // تصفير العداد بعد القفل
                    await _context.SaveChangesAsync();
                    ModelState.AddModelError("", "Too many wrong attempts. Account locked for 5 minutes.");
                }
                else
                {
                    await _context.SaveChangesAsync();
                    ModelState.AddModelError("", $"Wrong PIN. {5 - user.FailedPinCount} attempts remaining.");
                }

                vm.Users = await _context.TeamMembers.Where(t => t.IsActive).ToListAsync();
                return View(vm);
            }

            // 3. نجاح الدخول: تصفير العداد
            user.FailedPinCount = 0;
            user.LockedUntil = null;

            // تسجيل الدخول في ActivityLog
            var log = new ActivityLog
            {
                TeamMemberId = user.Id,
                Action = "Login",
                Details = "Login successful",
                CreatedAt = DateTime.Now
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();

            // 4. إنشاء الكوكيز (Claims)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // يبقى مسجل الدخول حتى لو أغلق المتصفح
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
       
        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
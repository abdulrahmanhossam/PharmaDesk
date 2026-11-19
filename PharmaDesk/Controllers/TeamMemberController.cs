using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Data;
using PharmaDesk.Models;
using PharmaDesk.ViewModels;

namespace PharmaDesk.Controllers
{
    // نحمي الكنترولر بالكامل: الأدمن فقط هو من يستطيع الدخول هنا
    [Authorize(Roles = "Admin")]
    public class TeamMemberController : Controller
    {
        private readonly PharmaDeskContext _context;

        public TeamMemberController(PharmaDeskContext context)
        {
            _context = context;
        }

        // GET: /TeamMember
        public async Task<IActionResult> Index()
        {
            var users = await _context.TeamMembers
                .OrderBy(t => t.FullName)
                .ToListAsync();

            return View(users);
        }

        // GET: /TeamMember/Create
        public IActionResult Create()
        {
            return View(new TeamMemberViewModel());
        }

        // POST: /TeamMember/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeamMemberViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.NewPin))
            {
                ModelState.AddModelError("NewPin", "PIN is required for new users.");
            }

            if (ModelState.IsValid)
            {
                var user = new TeamMember
                {
                    FullName = vm.FullName,
                    Role = vm.Role,
                    IsActive = vm.IsActive,
                    CreatedAt = DateTime.Now
                };

                // تشفير الـ PIN
                var hasher = new PasswordHasher<TeamMember>();
                user.PinHash = hasher.HashPassword(user, vm.NewPin!);

                _context.TeamMembers.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        // GET: /TeamMember/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.TeamMembers.FindAsync(id);
            if (user == null) return NotFound();

            var vm = new TeamMemberViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                NewPin = null // لا نعرض الـ PIN القديم أبداً
            };
            return View(vm);
        }

        // POST: /TeamMember/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TeamMemberViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _context.TeamMembers.FindAsync(id);
                if (user == null) return NotFound();

                user.FullName = vm.FullName;
                user.Role = vm.Role;
                user.IsActive = vm.IsActive;

                // إذا كتب الأدمن PIN جديداً، نقوم بتشفيره وتحديثه
                if (!string.IsNullOrWhiteSpace(vm.NewPin))
                {
                    var hasher = new PasswordHasher<TeamMember>();
                    user.PinHash = hasher.HashPassword(user, vm.NewPin);
                    user.FailedPinCount = 0; // تصفير المحاولات الفاشلة
                    user.LockedUntil = null; // فك القفل إذا كان مقفولاً
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        // POST: /TeamMember/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.TeamMembers.FindAsync(id);
            if (user == null) return NotFound();

            // منع الأدمن من إخفاء حسابه الخاص (حتى لا يقفل نفسه)
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
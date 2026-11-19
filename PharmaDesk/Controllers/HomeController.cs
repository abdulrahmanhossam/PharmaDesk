using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Data;

namespace PharmaDesk.Controllers
{
    [Authorize] // يجب تسجيل الدخول للوصول للوحة التحكم
    public class HomeController : Controller
    {
        private readonly PharmaDeskContext _context;

        public HomeController(PharmaDeskContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var startOfDay = DateTime.Today;
            var endOfDay = DateTime.Today.AddDays(1).AddTicks(-1);
            var ninetyDaysFromNow = today.AddDays(90);

            // 1. مبيعات اليوم (المبلغ الإجمالي)
            var todaySalesTotal = await _context.Sales
                .Where(s => s.SaleDate >= startOfDay && s.SaleDate <= endOfDay)
                .SumAsync(s => s.TotalAmount);

            // 2. عدد الفواتير اليوم
            var todaySalesCount = await _context.Sales
                .CountAsync(s => s.SaleDate >= startOfDay && s.SaleDate <= endOfDay);

            // 3. عدد الأدوية منخفضة المخزون
            var lowStockCount = await _context.Medicines
                .Where(m => m.IsActive)
                .Select(m => new
                {
                    m.LowStockLevel,
                    TotalStock = m.Batches.Where(b => b.ExpiryDate > today).Sum(b => b.QuantityInStock)
                })
                .CountAsync(x => x.TotalStock <= x.LowStockLevel);

            // 4. الأدوية القريبة من انتهاء الصلاحية (أو المنتهية)
            var expiringBatches = await _context.Batches
                .Include(b => b.Medicine)
                .Where(b => b.ExpiryDate <= ninetyDaysFromNow && b.QuantityInStock > 0)
                .OrderBy(b => b.ExpiryDate)
                .Take(10) // نعرض أول 10 فقط في اللوحة
                .ToListAsync();

            ViewBag.TodaySalesTotal = todaySalesTotal;
            ViewBag.TodaySalesCount = todaySalesCount;
            ViewBag.LowStockCount = lowStockCount;
            ViewBag.ExpiringBatches = expiringBatches;
            ViewBag.Today = today;

            // تخصيص الترحيب باسم المستخدم
            ViewData["Greeting"] = $"Good morning, {User.Identity?.Name}";

            return View();
        }
    }
}
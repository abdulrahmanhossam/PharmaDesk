using Microsoft.EntityFrameworkCore;
using PharmaDesk.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace PharmaDesk
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Add services to the container.
            builder.Services.AddControllersWithViews();

            // تسجيل SaleService
            builder.Services.AddScoped<PharmaDesk.Services.SaleService>();

            // Register DbContext
            builder.Services.AddDbContext<PharmaDeskContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 👇👇👇 هنا المكان الصح لإعدادات الـ Authentication (قبل الـ Build)
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(3);
                    options.SlidingExpiration = true;
                });
            // 👆👆👆

            var app = builder.Build(); // هنا بنقفل الخدمات

            // تشغيل Seeder عند بدء التطبيق
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<PharmaDeskContext>();
                DbSeeder.Seed(context);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication(); // لازم تكون قبل UseAuthorization

            // الميدل وير بتاعك لحماية الصفحات
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/Account") ||
                    context.Request.Path.StartsWithSegments("/css") ||
                    context.Request.Path.StartsWithSegments("/js") ||
                    context.Request.Path.StartsWithSegments("/lib"))
                {
                    await next();
                    return;
                }

                if (!context.User.Identity.IsAuthenticated)
                {
                    context.Response.Redirect("/Account/Login");
                    return;
                }
                await next();
            });

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
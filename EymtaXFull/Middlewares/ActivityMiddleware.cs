using Eymta.Repository.Data;
using System.Security.Claims;

namespace EymtaXFull.Middlewares
{
    public class ActivityTrackingMiddleware
    {
        private readonly RequestDelegate _next;

        public ActivityTrackingMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // جرب ندور على الـ ID بأكثر من طريقة عشان نتأكد
                var claim = context.User.FindFirstValue("sub")
                         ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (int.TryParse(claim, out int userId))
                {
                    var user = await db.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.LastSeenAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();

                        // سطر تجريبي عشان تعرف إن الكود اشتغل (شوف الـ Output في Visual Studio)
                        Console.WriteLine($"[ActivityTracking] Updated LastSeenAt for User {user.Username}");
                    }
                    else
                    {
                        Console.WriteLine($"[ActivityTracking] User with ID {userId} not found in DB.");
                    }
                }
                else
                {
                    Console.WriteLine("[ActivityTracking] Could not parse User ID from claims.");
                }
            }

            await _next(context);
        }
    }
}

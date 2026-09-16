using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Middleware
{
    // Records one PageView row per real page request, so the admin dashboard's
    // "Traffic" line can show actual visits instead of the sample data it used
    // to. Registered after UseRouting in Program.cs so ShouldCount sees the
    // final response status once the request has actually been handled.
    public class PageViewMiddleware
    {
        private readonly RequestDelegate _next;

        public PageViewMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, StoreDbContext db)
        {
            await _next(context);

            if (!ShouldCount(context)) return;

            db.PageViews.Add(new PageView
            {
                Path = context.Request.Path.Value ?? "/",
                CreatedAt = DateTime.UtcNow
            });

            // Best-effort: a logging failure should never take down the actual
            // page the visitor just successfully got served.
            try
            {
                await db.SaveChangesAsync();
            }
            catch
            {
                // Swallowed intentionally — see comment above.
            }
        }

        private static bool ShouldCount(HttpContext context)
        {
            if (context.Request.Method != HttpMethods.Get) return false;
            if (context.Response.StatusCode != StatusCodes.Status200OK) return false;

            var path = context.Request.Path.Value ?? "";

            // An admin loading their own dashboard shouldn't inflate the
            // "website traffic" number that same dashboard is showing them.
            if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)) return false;

            // Not a page render — nothing a real visitor "viewed".
            if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)) return false;

            // Static assets are normally served (and short-circuited) by
            // UseStaticFiles before this middleware ever runs, since it's
            // registered after that in the pipeline — this is just a defensive
            // second check for anything file-like that reaches here anyway.
            if (System.IO.Path.HasExtension(path)) return false;

            return true;
        }
    }
}

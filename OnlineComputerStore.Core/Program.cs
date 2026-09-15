using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Services;

// Pin currency/date formatting to en-US ("$1,899.00") regardless of the host
// machine's regional settings, so prices render consistently everywhere.
var storeCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = storeCulture;
CultureInfo.DefaultThreadCurrentUICulture = storeCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var provider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
builder.Services.AddDbContext<StoreDbContext>(options =>
{
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite"));
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Login attempts are limited per client IP: 5 tries per 5-minute window, so a
// brute-force guesser gets throttled without punishing everyone sharing the
// site if one person mistypes their password a few times.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0
        }));

    // Tighter than login: each request here actually sends an email through
    // our own SMTP account, and the endpoint could otherwise be used to
    // spam an inbox or probe which addresses have accounts registered.
    options.AddPolicy("forgot-password", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/plain";
        await context.HttpContext.Response.WriteAsync("Too many attempts. Please wait a few minutes and try again.", token);
    };
});

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddHttpClient<IPaymentService, StripePaymentService>();

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai"));
builder.Services.AddHttpClient<IAiSearchService, AiSearchService>();
builder.Services.AddHttpClient<IAiAssistantService, AiAssistantService>();
builder.Services.AddHttpClient<IAiContentService, AiContentService>();
builder.Services.AddHttpClient<IAiComparisonService, AiComparisonService>();
builder.Services.AddHttpClient<IAiOrderExplainService, AiOrderExplainService>();
builder.Services.AddHttpClient<IAiHomepageService, AiHomepageService>();
builder.Services.AddHttpClient<IAiUpsellService, AiUpsellService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    DbInitializer.Initialize(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

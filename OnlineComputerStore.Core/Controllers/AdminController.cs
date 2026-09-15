using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    // Only signed-in accounts with the Admin role get past this — previously this
    // whole controller (product management, AI description regeneration) was open
    // to anyone who typed /Admin/Products into the address bar.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private static readonly string[] ValidStatuses = { "Processing", "Shipped", "Delivered", "Cancelled" };

        private readonly StoreDbContext _db;
        private readonly IAiContentService _ai;
        private readonly IOrderService _orders;
        private readonly IAuditLogService _audit;

        public AdminController(StoreDbContext db, IAiContentService ai, IOrderService orders, IAuditLogService audit)
        {
            _db = db;
            _ai = ai;
            _orders = orders;
            _audit = audit;
        }

        public IActionResult Products() => View(_db.Products.OrderBy(p => p.Name).ToList());

        [HttpPost]
        public async Task<IActionResult> GenerateDescription(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Description = await _ai.GenerateDescriptionAsync(product);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentAdminId, CurrentAdminName, "Generated AI description", $"Product: {product.Name} (#{product.Id})");
            return RedirectToAction("Products");
        }

        // Sets a product's stock on hand to an exact new total — how an admin
        // restocks an out-of-stock item (or corrects a count after a stocktake).
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int id, int quantity)
        {
            if (quantity < 0) quantity = 0;

            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            var oldQuantity = product.StockQuantity;
            product.StockQuantity = quantity;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentAdminId, CurrentAdminName, "Updated stock", $"Product: {product.Name} (#{product.Id}): {oldQuantity} → {quantity}");
            return RedirectToAction("Products");
        }

        public async Task<IActionResult> Orders(string? status)
        {
            var orders = await _orders.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(status) && ValidStatuses.Contains(status))
                orders = orders.Where(o => o.Status == status).ToList();

            ViewBag.StatusFilter = status;
            ViewBag.Statuses = ValidStatuses;
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            if (!ValidStatuses.Contains(status)) return BadRequest();

            var order = await _orders.GetByIdAsync(id);
            var oldStatus = order?.Status;
            var success = await _orders.UpdateStatusAsync(id, status);

            if (success)
                await _audit.LogAsync(CurrentAdminId, CurrentAdminName, "Updated order status", $"Order #{id}: {oldStatus} → {status}");

            return RedirectToAction("Orders");
        }

        public async Task<IActionResult> Dashboard()
        {
            var orders = await _orders.GetAllAsync();

            ViewBag.TotalRevenue = orders.Sum(o => o.Total);
            ViewBag.TotalOrders = orders.Count;
            ViewBag.OrdersByStatus = orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count());
            ViewBag.RecentOrders = orders.Take(5).ToList();
            ViewBag.TopProducts = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductName)
                .Select(g => new { Name = g.Key, Quantity = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Quantity * i.UnitPrice) })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList();

            return View();
        }

        public async Task<IActionResult> AuditLog() => View(await _audit.GetRecentAsync());

        // Wipes the audit log so it doesn't just grow forever — leaves one fresh
        // entry behind recording that it was cleared, and by whom.
        [HttpPost]
        public async Task<IActionResult> ClearAuditLog()
        {
            await _audit.ClearAllAsync(CurrentAdminId, CurrentAdminName);
            return RedirectToAction("AuditLog");
        }

        // Rolling retention clear: removes entries older than the given number of
        // days, leaving recent activity in place.
        [HttpPost]
        public async Task<IActionResult> ClearAuditLogByDays(int days)
        {
            if (days > 0)
                await _audit.ClearOlderThanAsync(days, CurrentAdminId, CurrentAdminName);

            return RedirectToAction("AuditLog");
        }

        // Removes only the entries from one chosen calendar month, via an
        // <input type="month"> value (format "yyyy-MM").
        [HttpPost]
        public async Task<IActionResult> ClearAuditLogByMonth(string month)
        {
            if (DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                await _audit.ClearByMonthAsync(parsed.Year, parsed.Month, CurrentAdminId, CurrentAdminName);

            return RedirectToAction("AuditLog");
        }

        // Downloads the (optionally status-filtered) order list as a CSV — for
        // pulling sales data into Excel/Sheets without touching the database directly.
        [HttpGet]
        public async Task<IActionResult> ExportOrdersCsv(string? status)
        {
            var orders = await _orders.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(status) && ValidStatuses.Contains(status))
                orders = orders.Where(o => o.Status == status).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("OrderId,CustomerName,Email,Items,Total,PaymentProvider,PaymentStatus,Fulfillment,PickupLocation,Address,Status,PlacedAtUtc");
            foreach (var o in orders)
            {
                var items = string.Join(" | ", o.Items.Select(i => $"{i.ProductName} x{i.Quantity}"));
                sb.AppendLine(string.Join(",",
                    o.Id,
                    CsvField(o.CustomerName),
                    CsvField(o.Email),
                    CsvField(items),
                    o.Total.ToString(CultureInfo.InvariantCulture),
                    CsvField(o.PaymentProvider),
                    CsvField(o.PaymentStatus),
                    CsvField(o.FulfillmentMethod),
                    CsvField(o.PickupLocation ?? ""),
                    CsvField(o.Address),
                    CsvField(o.Status),
                    o.CreatedAt.ToString("u", CultureInfo.InvariantCulture)));
            }

            await _audit.LogAsync(CurrentAdminId, CurrentAdminName, "Exported orders CSV",
                $"{orders.Count} order(s)" + (string.IsNullOrWhiteSpace(status) ? "" : $", status: {status}"));

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"orders-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }

        // Quotes a CSV field only when it needs it (contains a comma, quote, or newline),
        // escaping embedded quotes by doubling them per the standard CSV convention.
        private static string CsvField(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private int CurrentAdminId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string CurrentAdminName => User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
    }
}

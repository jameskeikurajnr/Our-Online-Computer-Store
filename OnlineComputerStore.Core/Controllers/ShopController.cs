using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Models;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    public class ShopController : Controller
    {
        private const int PageSize = 8;

        // Categories treated as "a device" for the Complete Your Setup bundle,
        // and the essentials categories it pulls one pick from for each.
        private static readonly string[] DeviceCategories = { "Laptops", "Desktops", "Tablets" };
        private static readonly string[] SetupBundleCategories = { "MFA & Security Keys", "Cybersecurity", "Chargers", "USB Drives" };

        private readonly IProductService _products;
        private readonly IAiUpsellService _upsell;
        private readonly IWishlistService _wishlist;
        private readonly IReviewService _reviews;
        private readonly IOrderService _orders;

        public ShopController(IProductService products, IAiUpsellService upsell, IWishlistService wishlist, IReviewService reviews, IOrderService orders)
        {
            _products = products;
            _upsell = upsell;
            _wishlist = wishlist;
            _reviews = reviews;
            _orders = orders;
        }

        public async Task<IActionResult> Index(string? sort = "name", int page = 1)
        {
            var all = (await _products.GetAllAsync()).ToList();

            all = sort switch
            {
                "price_asc" => all.OrderBy(p => p.Price).ToList(),
                "price_desc" => all.OrderByDescending(p => p.Price).ToList(),
                "brand" => all.OrderBy(p => p.Brand).ThenBy(p => p.Name).ToList(),
                _ => all.OrderBy(p => p.Name).ToList()
            };

            var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)PageSize));
            page = Math.Max(1, Math.Min(page, totalPages));
            var pageItems = all.Skip((page - 1) * PageSize).Take(PageSize).ToList();

            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.WishlistIds = await GetWishlistIdsAsync();

            // Next 7 from the same trending ranking Home takes its top 3 from
            // (skipping those 3), so this shelf never repeats what's on Home —
            // and only on page 1, so it doesn't reappear while paging through the grid.
            ViewBag.Trending = page == 1
                ? (await _products.GetTrendingAsync()).Skip(3).Take(7).ToList()
                : new List<Product>();

            return View(pageItems);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _products.GetByIdAsync(id);
            if (product == null) return NotFound();

            TrackBrowseHistory(product.Name);

            ViewBag.Upsell = await _upsell.SuggestUpsellAsync(product);
            // Real co-purchase data from past orders, not AI-generated — a stronger
            // signal than the upsell text above once there's enough order history.
            ViewBag.FrequentlyBoughtTogether = await _orders.GetFrequentlyBoughtTogetherAsync(product.Id);

            // "Complete Your Setup": a curated, one-click-add bundle shown only on
            // devices (laptops/desktops/tablets) — one in-stock pick from each
            // essentials category, so a new machine leaves the store secured,
            // powered, and backed up. Distinct from the AI text upsell above: this
            // is a real form that adds every ticked item straight to the cart.
            if (DeviceCategories.Contains(product.Category))
            {
                var all = await _products.GetAllAsync();
                ViewBag.SetupBundle = SetupBundleCategories
                    .Select(cat => all
                        .Where(p => p.Category == cat && p.StockQuantity > 0)
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenBy(p => p.Price)
                        .FirstOrDefault())
                    .Where(p => p != null)
                    .ToList();
            }
            else
            {
                ViewBag.SetupBundle = new List<Product>();
            }

            var wishlistIds = await GetWishlistIdsAsync();
            ViewBag.InWishlist = wishlistIds.Contains(product.Id);

            ViewBag.Reviews = await _reviews.GetForProductAsync(id);
            var (average, count) = await _reviews.GetRatingSummaryAsync(id);
            ViewBag.RatingAverage = average;
            ViewBag.RatingCount = count;

            var userId = CurrentUserId;
            ViewBag.HasReviewed = userId.HasValue && await _reviews.HasReviewedAsync(id, userId.Value);

            return View(product);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var userId = CurrentUserId!.Value;
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Customer";

            var (success, message) = await _reviews.AddReviewAsync(productId, userId, userName, rating, comment);
            if (!success)
                TempData["ReviewError"] = message;

            return RedirectToAction("Details", new { id = productId });
        }

        private int? CurrentUserId
        {
            get
            {
                if (User.Identity == null || !User.Identity.IsAuthenticated) return null;
                var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(idClaim, out var id) ? id : null;
            }
        }

        private async Task<HashSet<int>> GetWishlistIdsAsync()
        {
            var userId = CurrentUserId;
            return userId.HasValue ? await _wishlist.GetProductIdsAsync(userId.Value) : new HashSet<int>();
        }

        private void TrackBrowseHistory(string productName)
        {
            var existing = HttpContext.Session.GetString("BrowseHistory");
            var items = string.IsNullOrWhiteSpace(existing)
                ? new List<string>()
                : existing.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();

            items.Remove(productName); // avoid clutter if they revisit the same product
            items.Add(productName);

            // Keep only the 5 most recently viewed products, so the persona reflects current interest.
            if (items.Count > 5)
            {
                items = items.Skip(items.Count - 5).ToList();
            }

            HttpContext.Session.SetString("BrowseHistory", string.Join(", ", items));
        }
    }
}
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Components
{
    // Mirrors CartBadgeViewComponent's pattern — re-runs on every page so the
    // wishlist count on the navbar heart icon is always accurate. Renders 0 (i.e.
    // nothing) for guests, since the wishlist is account-only.
    public class WishlistBadgeViewComponent : ViewComponent
    {
        private readonly IWishlistService _wishlist;
        public WishlistBadgeViewComponent(IWishlistService wishlist) => _wishlist = wishlist;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = HttpContext.User;
            if (user?.Identity == null || !user.Identity.IsAuthenticated) return View(0);

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId)) return View(0);

            var ids = await _wishlist.GetProductIdsAsync(userId);
            return View(ids.Count);
        }
    }
}

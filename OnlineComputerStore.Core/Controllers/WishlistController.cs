using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlist;
        public WishlistController(IWishlistService wishlist) => _wishlist = wishlist;

        public async Task<IActionResult> Index() => View(await _wishlist.GetProductsAsync(CurrentUserId));

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
        {
            await _wishlist.ToggleAsync(CurrentUserId, productId);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        // Empties the whole wishlist in one go, mirroring the "Clear Cart" button
        // on the cart page.
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            await _wishlist.ClearAsync(CurrentUserId);
            return RedirectToAction("Index");
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

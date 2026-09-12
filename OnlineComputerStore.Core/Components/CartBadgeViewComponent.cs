using System.Linq;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Components
{
    // Renders the little item-count badge on the navbar's cart icon. As a view
    // component it re-runs on every page request no matter which controller is
    // handling it, so the count on the Home page, Shop page, etc. is always
    // accurate without every controller needing to remember to set it.
    public class CartBadgeViewComponent : ViewComponent
    {
        private readonly ICartService _cart;

        public CartBadgeViewComponent(ICartService cart) => _cart = cart;

        public IViewComponentResult Invoke()
        {
            var itemCount = _cart.GetCart().Items.Sum(i => i.Quantity);
            return View(itemCount);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cart;
        public CartController(ICartService cart) => _cart = cart;

        [HttpPost]
        public IActionResult Add(int productId, int quantity = 1)
        {
            _cart.Add(productId, quantity);
            return RedirectToAction("Index");
        }

        public IActionResult Index() => View(_cart.GetCart());

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            _cart.UpdateQuantity(productId, quantity);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            _cart.Remove(productId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Clear()
        {
            _cart.Clear();
            return RedirectToAction("Index");
        }
    }
}

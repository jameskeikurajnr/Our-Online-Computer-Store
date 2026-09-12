using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orders;
        private readonly IAiOrderExplainService _ai;

        public OrdersController(IOrderService orders, IAiOrderExplainService ai)
        {
            _orders = orders;
            _ai = ai;
        }

        [HttpGet]
        public IActionResult Track() => View();

        [HttpPost]
        public async Task<IActionResult> Track(int orderId, string email)
        {
            // Order IDs are short and guessable, so we also require the email used
            // at checkout to match before showing anyone's order details. This keeps
            // one customer from browsing another customer's order just by trying IDs.
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Please enter the email address used for this order.");
                return View();
            }

            var order = await _orders.GetByIdAsync(orderId);
            if (order == null || !string.Equals(order.Email?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                // Deliberately generic: don't reveal whether the order ID exists but the
                // email didn't match, versus the order not existing at all.
                ModelState.AddModelError("", "We couldn't find an order with that ID and email combination.");
                return View();
            }

            ViewBag.Order = order;
            ViewBag.Explanation = await _ai.ExplainStatusAsync(order);
            return View("TrackResult");
        }
    }
}

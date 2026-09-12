using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface IOrderService
    {
        Task<Order> PlaceOrderAsync(
            CheckoutViewModel details,
            Cart cart,
            string paymentProvider = "None",
            string paymentStatus = "Not Required",
            string? paymentReference = null);

        Task<Order?> GetByIdAsync(int id);

        // Used to avoid placing a second order if a customer refreshes/revisits the
        // post-payment success page for a Stripe session that's already been fulfilled.
        Task<Order?> GetByPaymentReferenceAsync(string paymentReference);

        // Admin: full order list (newest first) for the Orders/Dashboard views.
        Task<List<Order>> GetAllAsync();

        Task<bool> UpdateStatusAsync(int id, string status);

        // "Customers who bought this also bought": other products that show up most
        // often in past orders alongside productId, ranked by co-purchase frequency.
        Task<List<Product>> GetFrequentlyBoughtTogetherAsync(int productId, int take = 4);
    }
}

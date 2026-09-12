using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public class OrderService : IOrderService
    {
        // A product is "low stock" once it drops to or below this many units — same
        // threshold the shop/admin UI already uses for the amber "Low: X left" badge.
        private const int LowStockThreshold = 5;

        private readonly StoreDbContext _context;
        private readonly IEmailService _email;
        private static readonly Random _random = new();

        public OrderService(StoreDbContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

        public async Task<Order> PlaceOrderAsync(
            CheckoutViewModel details,
            Cart cart,
            string paymentProvider = "None",
            string paymentStatus = "Not Required",
            string? paymentReference = null)
        {
            var order = new Order
            {
                Id = await GenerateUniqueOrderNumberAsync(),
                CustomerName = details.CustomerName,
                Email = details.Email,
                Address = details.Address,
                FulfillmentMethod = details.FulfillmentMethod,
                PickupLocation = details.PickupLocation,
                Status = "Processing",
                Total = cart.Total,
                PaymentProvider = paymentProvider,
                PaymentStatus = paymentStatus,
                PaymentReference = paymentReference
            };

            // Products that cross into (or further below) the low-stock threshold as a
            // direct result of this order — collected so we can alert admin once this
            // order is safely saved, not while still mid-transaction.
            var newlyLowStock = new List<Product>();

            foreach (var item in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity
                });

                // Best-effort inventory decrement — clamped at 0 rather than going
                // negative. CartService already caps how much can be added per
                // product to the stock on hand, so this is a backstop, not the
                // only place oversell is prevented.
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var stockBefore = product.StockQuantity;
                    product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);

                    // Only alert on the transition into low stock, not on every order
                    // placed while it's already low — otherwise a popular low-stock
                    // item would spam an email per sale.
                    if (stockBefore > LowStockThreshold && product.StockQuantity <= LowStockThreshold)
                        newlyLowStock.Add(product);
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (newlyLowStock.Count > 0)
                await _email.SendLowStockAlertAsync(newlyLowStock);

            return order;
        }

        public async Task<Order?> GetByIdAsync(int id) =>
            await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

        public async Task<Order?> GetByPaymentReferenceAsync(string paymentReference) =>
            await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.PaymentReference == paymentReference);

        public async Task<List<Order>> GetAllAsync() =>
            await _context.Orders.Include(o => o.Items).OrderByDescending(o => o.CreatedAt).ToListAsync();

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return false;

            order.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Product>> GetFrequentlyBoughtTogetherAsync(int productId, int take = 4)
        {
            // Every order that included this product...
            var orderIds = await _context.OrderItems
                .Where(oi => oi.ProductId == productId)
                .Select(oi => oi.OrderId)
                .Distinct()
                .ToListAsync();

            if (orderIds.Count == 0) return new List<Product>();

            // ...ranked by how often each *other* product showed up alongside it.
            var coProductIds = await _context.OrderItems
                .Where(oi => orderIds.Contains(oi.OrderId) && oi.ProductId != productId)
                .GroupBy(oi => oi.ProductId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(take)
                .ToListAsync();

            if (coProductIds.Count == 0) return new List<Product>();

            var products = await _context.Products
                .Where(p => coProductIds.Contains(p.Id))
                .ToListAsync();

            // Preserve the popularity ranking from coProductIds — the DB query above
            // doesn't guarantee row order survives the second query.
            return coProductIds
                .Select(id => products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .ToList()!;
        }

        private async Task<int> GenerateUniqueOrderNumberAsync()
        {
            int candidate;
            do
            {
                candidate = _random.Next(10000, 100000); // random 5-digit number: 10000–99999
            }
            while (await _context.Orders.AnyAsync(o => o.Id == candidate));

            return candidate;
        }
    }
}

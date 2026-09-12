using System;
using System.Collections.Generic;

namespace OnlineComputerStore.Core.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Processing";
        public List<OrderItem> Items { get; set; } = new();
        public decimal Total { get; set; }

        // "Ship" (the original/default behaviour — deliver to Address) or "Pickup"
        // (Click & Collect — the customer collects in person at PickupLocation).
        public string FulfillmentMethod { get; set; } = "Ship";
        public string? PickupLocation { get; set; }

        // Payment tracking — separate from Status (the fulfillment/shipping state).
        // PaymentProvider is "None" for orders placed while Stripe isn't configured,
        // so the site keeps working without payment set up (same pattern as Ai/Smtp).
        public string PaymentProvider { get; set; } = "None";
        public string PaymentStatus { get; set; } = "Not Required";
        // Stripe Checkout Session id (cs_...) — used to look the payment back up on
        // Stripe's side and to guard against creating a duplicate order if the
        // customer refreshes or revisits the success page.
        public string? PaymentReference { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}

using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface IPaymentService
    {
        bool IsConfigured { get; }

        // Creates a Stripe-hosted Checkout Session for the given cart and returns the
        // URL to redirect the customer's browser to. Card details are entered and
        // tokenized on Stripe's own page — this app never sees a raw card number.
        Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Cart cart, string customerEmail, string successUrl, string cancelUrl);

        // Server-side lookup used after Stripe redirects the customer back, so the
        // order is only ever placed once Stripe itself confirms payment succeeded —
        // never based on the redirect happening at all (that could be faked/replayed).
        Task<CheckoutSessionStatus> GetSessionStatusAsync(string sessionId);
    }

    public class CheckoutSessionResult
    {
        public bool Success { get; set; }
        public string? Url { get; set; }
        public string? SessionId { get; set; }
        public string? Error { get; set; }
    }

    public class CheckoutSessionStatus
    {
        public bool Found { get; set; }
        public bool IsPaid { get; set; }
        public string PaymentStatus { get; set; } = "";
        public decimal AmountTotal { get; set; }
        public string? CustomerEmail { get; set; }
        public string? Error { get; set; }
    }
}

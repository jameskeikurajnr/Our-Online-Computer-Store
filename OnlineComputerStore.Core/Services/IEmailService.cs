using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface IEmailService
    {
        bool IsConfigured { get; }
        Task<(bool Success, string Message)> SendContactMessageAsync(string fromName, string fromEmail, string subject, string body);
        Task SendOrderConfirmationAsync(Order order);
        Task SendPasswordResetAsync(AppUser user, string resetUrl);

        // Unlike the other Send* methods, this one is on the critical path of
        // checkout: the caller needs to know whether it actually went out so it
        // can tell the customer, rather than swallowing the failure.
        Task<(bool Success, string Message)> SendCheckoutVerificationCodeAsync(string toEmail, string code);

        // Admin-facing, not customer-facing — sent to the store's own ToAddress
        // whenever an order pushes one or more products at/below the low-stock
        // threshold, so restocking doesn't rely on someone remembering to check.
        Task SendLowStockAlertAsync(List<Product> products);

        // Admin-facing, not customer-facing — sent to the store's own ToAddress
        // the moment an unmatched search term has been searched more than 3
        // times, so the admin knows what to go buy and list. Not sent on every
        // single miss — only once a term looks like a real pattern rather than
        // a one-off typo or curiosity search.
        Task SendProductRequestAlertAsync(string searchTerm, int timesSearched);
    }
}
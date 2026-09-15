using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Tests.TestHelpers
{
    // No-op IEmailService so service tests (OrderService, etc.) can run without real
    // SMTP config. Records the last low-stock alert so a test can assert on it if needed.
    public class FakeEmailService : IEmailService
    {
        public bool IsConfigured => true;
        public List<Product> LastLowStockAlert { get; private set; } = new();

        public Task<(bool Success, string Message)> SendContactMessageAsync(string fromName, string fromEmail, string subject, string body) =>
            Task.FromResult((true, "sent"));

        public Task SendOrderConfirmationAsync(Order order) => Task.CompletedTask;

        public Task SendPasswordResetAsync(AppUser user, string resetUrl) => Task.CompletedTask;

        public Task<(bool Success, string Message)> SendCheckoutVerificationCodeAsync(string toEmail, string code) =>
            Task.FromResult((true, "sent"));

        public Task SendLowStockAlertAsync(List<Product> products)
        {
            LastLowStockAlert = products;
            return Task.CompletedTask;
        }

        public string? LastProductRequestAlert { get; private set; }
        public int LastProductRequestAlertCount { get; private set; }

        public Task SendProductRequestAlertAsync(string searchTerm, int timesSearched)
        {
            LastProductRequestAlert = searchTerm;
            LastProductRequestAlertCount = timesSearched;
            return Task.CompletedTask;
        }
    }
}

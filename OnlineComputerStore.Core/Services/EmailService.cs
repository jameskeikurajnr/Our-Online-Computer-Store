using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured =>
            _options.Enabled &&
            !string.IsNullOrWhiteSpace(_options.Host) &&
            !string.IsNullOrWhiteSpace(_options.Username) &&
            !string.IsNullOrWhiteSpace(_options.Password) &&
            !string.IsNullOrWhiteSpace(_options.FromAddress) &&
            !string.IsNullOrWhiteSpace(_options.ToAddress);

        public async Task<(bool Success, string Message)> SendContactMessageAsync(string fromName, string fromEmail, string subject, string body)
        {
            if (!IsConfigured)
                return (false, "Email isn't configured yet — add your SMTP details in appsettings.json (see the \"Smtp\" section).");

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
                message.To.Add(MailboxAddress.Parse(_options.ToAddress));

                if (MailboxAddress.TryParse(fromEmail, out var replyTo))
                {
                    message.ReplyTo.Add(replyTo);
                }

                message.Subject = $"[Contact form] {subject}";
                message.Body = new TextPart("plain")
                {
                    Text = $"From: {fromName} <{fromEmail}>\n\n{body}"
                };

                using var client = new SmtpClient();
                await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.Username, _options.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return (true, "Message sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact form email via SMTP.");
                return (false, $"We couldn't send that message just now: {ex.Message}");
            }
        }

        public async Task SendOrderConfirmationAsync(Order order)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(order.Email)) return;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
                message.Subject = $"Your Online Computer Store order #{order.Id} is confirmed";

                var body = new StringBuilder();
                body.AppendLine($"Hi {order.CustomerName},");
                body.AppendLine();
                body.AppendLine($"Thanks for your order! Your order number is #{order.Id} — keep this number so you can track your order any time using the \"Track Order\" page on our site. God Bless you!");
                body.AppendLine();
                body.AppendLine("Order summary:");
                foreach (var item in order.Items)
                {
                    body.AppendLine($"  - {item.ProductName} x{item.Quantity} — {item.UnitPrice:C}");
                }
                body.AppendLine();
                body.AppendLine($"Total: {order.Total:C}");
                body.AppendLine();
                body.AppendLine(order.FulfillmentMethod == "Pickup"
                    ? $"Ready for collection at: {order.PickupLocation} — we'll email you when it's ready."
                    : $"Shipping to: {order.Address}");
                body.AppendLine();
                body.AppendLine("We'll let you know as your order progresses. Thanks for shopping with us! God Bless you.");

                // Sent as multipart/alternative: HtmlBody is what most inboxes show
                // (branded, on-brand colors), TextBody is the plain-text fallback
                // above for clients/screen readers that don't render HTML.
                message.Body = new BodyBuilder
                {
                    TextBody = body.ToString(),
                    HtmlBody = BuildOrderConfirmationHtml(order)
                }.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.Username, _options.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for order #{OrderId}.", order.Id);
                // Deliberately not rethrown — a failed confirmation email shouldn't stop checkout from completing.
            }
        }

        // A short, branded HTML version of the order confirmation — same info as the
        // plain-text body above, laid out as a compact card matching the site's colors
        // (navy header, teal accents). Table-based layout with inline styles only,
        // since that's what actually renders consistently across email clients
        // (Gmail, Outlook, etc. strip <style> blocks and most CSS).
        private static string BuildOrderConfirmationHtml(Order order)
        {
            string Esc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

            var itemRows = new StringBuilder();
            foreach (var item in order.Items)
            {
                itemRows.Append($@"
              <tr>
                <td style=""padding:8px 0;border-bottom:1px solid #eef0f4;color:#1c1c1e;"">{Esc(item.ProductName)} &times; {item.Quantity}</td>
                <td style=""padding:8px 0;border-bottom:1px solid #eef0f4;color:#1c1c1e;text-align:right;white-space:nowrap;"">{item.UnitPrice:C}</td>
              </tr>");
            }

            var fulfillment = order.FulfillmentMethod == "Pickup"
                ? $"Ready for collection at <strong>{Esc(order.PickupLocation)}</strong> — we'll email you when it's ready."
                : $"Shipping to <strong>{Esc(order.Address)}</strong>.";

            return $@"
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f7f8fb;padding:24px 0;"">
  <tr><td align=""center"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:560px;background:#ffffff;border-radius:12px;overflow:hidden;font-family:'Segoe UI',Roboto,Arial,sans-serif;"">
      <tr>
        <td style=""background:#14213d;padding:20px 28px;"">
          <table role=""presentation"" cellpadding=""0"" cellspacing=""0""><tr>
            <td style=""width:34px;height:32px;background:#14b8a6;border-radius:6px;color:#04221d;font-weight:700;font-size:12px;text-align:center;vertical-align:middle;"">OCS</td>
            <td style=""padding-left:10px;color:#ffffff;font-weight:600;font-size:16px;"">Online Computer Store</td>
          </tr></table>
        </td>
      </tr>
      <tr>
        <td style=""padding:28px;"">
          <h1 style=""margin:0 0 4px;font-size:20px;color:#14213d;"">Thanks, {Esc(order.CustomerName)}! God Bless you.</h1>
          <p style=""margin:0 0 20px;color:#5b6472;font-size:14px;"">Your order <strong>#{order.Id}</strong> is confirmed.</p>
          <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size:14px;border-collapse:collapse;"">
            {itemRows}
            <tr><td style=""padding:12px 0 0;font-weight:700;color:#14213d;"">Total</td><td style=""padding:12px 0 0;text-align:right;font-weight:700;color:#14213d;"">{order.Total:C}</td></tr>
          </table>
          <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:20px;background:#eefaf8;border-radius:8px;"">
            <tr><td style=""padding:14px 16px;font-size:13px;color:#0b3b34;"">{fulfillment}</td></tr>
          </table>
          <p style=""margin:20px 0 0;font-size:13px;color:#8a919c;"">Track anytime using your order number on the Track Order page. God Bless you</p>
        </td>
      </tr>
      <tr>
        <td style=""background:#f7f8fb;padding:16px 28px;text-align:center;font-size:11px;color:#9aa0a6;"">
          &copy; {DateTime.UtcNow.Year} Online Computer Store For All!
        </td>
      </tr>
    </table>
  </td></tr>
</table>";
        }

        public async Task SendPasswordResetAsync(AppUser user, string resetUrl)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(user.Email)) return;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
                message.To.Add(new MailboxAddress(user.FullName, user.Email));
                message.Subject = "Reset your Online Computer Store password";

                var body = new StringBuilder();
                body.AppendLine($"Hi {user.FullName},");
                body.AppendLine();
                body.AppendLine("We received a request to reset your password. Click the link below to choose a new one:");
                body.AppendLine();
                body.AppendLine(resetUrl);
                body.AppendLine();
                body.AppendLine("This link expires in 1 hour. If you didn't request this, you can safely ignore this email — your password won't be changed.");

                message.Body = new TextPart("plain") { Text = body.ToString() };

                using var client = new SmtpClient();
                await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.Username, _options.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email for user #{UserId}.", user.Id);
                // Deliberately not rethrown — ForgotPassword always shows the same
                // generic message regardless of email delivery, to avoid leaking
                // whether an address has an account.
            }
        }

        public async Task<(bool Success, string Message)> SendCheckoutVerificationCodeAsync(string toEmail, string code)
        {
            if (!IsConfigured)
                return (false, "Email verification isn't available right now — SMTP isn't configured (see the \"Smtp\" section in appsettings.json).");

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = "Your Online Computer Store verification code";

                var body = new StringBuilder();
                body.AppendLine($"Your checkout verification code is: {code}");
                body.AppendLine();
                body.AppendLine("This code expires in 10 minutes. If you didn't try to check out, you can safely ignore this email.");

                message.Body = new TextPart("plain") { Text = body.ToString() };

                using var client = new SmtpClient();
                await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.Username, _options.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return (true, "Code sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send checkout verification code to {Email}.", toEmail);
                return (false, "We couldn't send the verification code just now. Please try again in a moment.");
            }
        }

        public async Task SendLowStockAlertAsync(List<Product> products)
        {
            if (!IsConfigured || products.Count == 0) return;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
                message.To.Add(MailboxAddress.Parse(_options.ToAddress));
                message.Subject = products.Count == 1
                    ? $"Low stock: {products[0].Name}"
                    : $"Low stock alert: {products.Count} products need restocking";

                var body = new StringBuilder();
                body.AppendLine("The following products have just dropped to or below the low-stock threshold:");
                body.AppendLine();
                foreach (var p in products)
                {
                    body.AppendLine(p.StockQuantity <= 0
                        ? $"  - {p.Name} — OUT OF STOCK"
                        : $"  - {p.Name} — {p.StockQuantity} left");
                }
                body.AppendLine();
                body.AppendLine("Head to Admin > Products to restock.");

                message.Body = new TextPart("plain") { Text = body.ToString() };

                using var client = new SmtpClient();
                await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.Username, _options.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send low-stock alert email.");
                // Deliberately not rethrown — a failed alert shouldn't break checkout.
            }
        }
    }
}
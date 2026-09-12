using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    // Talks to Stripe's Checkout Sessions REST API directly over HttpClient — no
    // Stripe.net SDK/NuGet dependency, same "typed HttpClient + raw JSON" pattern
    // already used for the OpenAI-backed AI services (see AiServiceBase).
    //
    // Stripe Checkout is a Stripe-hosted payment page: the customer types their
    // card details on stripe.com, not on this site, so this server never receives,
    // transmits, or stores a raw card number — the same approach you'd want on a
    // real production store, not just a shortcut for the demo. Swapping the test
    // secret key (sk_test_...) for a live one (sk_live_...) in appsettings.json is
    // the only change needed to start taking real payments later.
    public class StripePaymentService : IPaymentService
    {
        private const string ApiBase = "https://api.stripe.com/v1";

        private readonly HttpClient _http;
        private readonly StripeOptions _options;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(HttpClient http, IOptions<StripeOptions> options, ILogger<StripePaymentService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured =>
            _options.Enabled && !string.IsNullOrWhiteSpace(_options.SecretKey);

        public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Cart cart, string customerEmail, string successUrl, string cancelUrl)
        {
            if (!IsConfigured)
            {
                return new CheckoutSessionResult
                {
                    Success = false,
                    Error = "Stripe isn't configured yet — add your API keys in appsettings.json (see the \"Stripe\" section)."
                };
            }

            var currency = string.IsNullOrWhiteSpace(_options.Currency) ? "usd" : _options.Currency.Trim().ToLowerInvariant();

            var form = new List<KeyValuePair<string, string>>
            {
                new("mode", "payment"),
                new("success_url", successUrl),
                new("cancel_url", cancelUrl),
            };

            if (!string.IsNullOrWhiteSpace(customerEmail))
                form.Add(new KeyValuePair<string, string>("customer_email", customerEmail));

            var i = 0;
            foreach (var item in cart.Items)
            {
                // Stripe amounts are integers in the currency's smallest unit
                // (e.g. cents for USD/AUD), never a decimal dollar amount.
                var unitAmount = (long)Math.Round(item.Product.Price * 100m, MidpointRounding.AwayFromZero);

                form.Add(new KeyValuePair<string, string>($"line_items[{i}][price_data][currency]", currency));
                form.Add(new KeyValuePair<string, string>($"line_items[{i}][price_data][product_data][name]", item.Product.Name));
                form.Add(new KeyValuePair<string, string>($"line_items[{i}][price_data][unit_amount]", unitAmount.ToString()));
                form.Add(new KeyValuePair<string, string>($"line_items[{i}][quantity]", item.Quantity.ToString()));
                i++;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/checkout/sessions")
                {
                    Content = new FormUrlEncodedContent(form)
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

                var response = await _http.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Stripe checkout session creation failed ({Status}): {Body}", response.StatusCode, body);
                    return new CheckoutSessionResult
                    {
                        Success = false,
                        Error = $"Stripe returned an error ({(int)response.StatusCode}). Check your Stripe keys in appsettings.json."
                    };
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var url = root.TryGetProperty("url", out var u) ? u.GetString() : null;
                var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

                if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(id))
                    return new CheckoutSessionResult { Success = false, Error = "Stripe didn't return a checkout URL." };

                return new CheckoutSessionResult { Success = true, Url = url, SessionId = id };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reach Stripe while creating a checkout session.");
                return new CheckoutSessionResult { Success = false, Error = "Couldn't reach the payment provider just now. Please try again shortly." };
            }
        }

        public async Task<CheckoutSessionStatus> GetSessionStatusAsync(string sessionId)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(sessionId))
                return new CheckoutSessionStatus { Found = false, Error = "Stripe isn't configured." };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/checkout/sessions/{Uri.EscapeDataString(sessionId)}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

                var response = await _http.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Stripe checkout session lookup failed ({Status}): {Body}", response.StatusCode, body);
                    return new CheckoutSessionStatus { Found = false, Error = $"Stripe returned an error ({(int)response.StatusCode})." };
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var paymentStatus = root.TryGetProperty("payment_status", out var ps) ? ps.GetString() ?? "" : "";
                var amountTotalCents = root.TryGetProperty("amount_total", out var at) && at.ValueKind == JsonValueKind.Number
                    ? at.GetInt64()
                    : 0L;

                string? email = null;
                if (root.TryGetProperty("customer_details", out var cd) && cd.ValueKind == JsonValueKind.Object
                    && cd.TryGetProperty("email", out var em))
                {
                    email = em.GetString();
                }

                return new CheckoutSessionStatus
                {
                    Found = true,
                    IsPaid = paymentStatus == "paid",
                    PaymentStatus = paymentStatus,
                    AmountTotal = amountTotalCents / 100m,
                    CustomerEmail = email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reach Stripe while checking a checkout session.");
                return new CheckoutSessionStatus { Found = false, Error = "Couldn't reach the payment provider just now." };
            }
        }
    }
}

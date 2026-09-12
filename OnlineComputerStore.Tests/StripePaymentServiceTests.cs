using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnlineComputerStore.Core.Models;
using OnlineComputerStore.Core.Services;
using OnlineComputerStore.Tests.TestHelpers;
using Xunit;

namespace OnlineComputerStore.Tests
{
    public class StripePaymentServiceTests
    {
        private static StripePaymentService BuildService(StripeOptions options, HttpMessageHandler? handler = null)
        {
            var http = new HttpClient(handler ?? new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }))
            {
                BaseAddress = new Uri("https://api.stripe.com/")
            };
            return new StripePaymentService(http, Options.Create(options), NullLogger<StripePaymentService>.Instance);
        }

        private static Cart OneItemCart(decimal price = 19.99m)
        {
            var product = new Product { Id = 1, Name = "Widget", Price = price };
            return new Cart { Items = { new CartItem { ProductId = 1, Product = product, Quantity = 1 } } };
        }

        [Fact]
        public void IsConfigured_FalseWhenDisabled_EvenWithAKeySet()
        {
            var sut = BuildService(new StripeOptions { Enabled = false, SecretKey = "sk_test_x" });
            Assert.False(sut.IsConfigured);
        }

        [Fact]
        public void IsConfigured_FalseWhenNoSecretKey_EvenIfEnabled()
        {
            var sut = BuildService(new StripeOptions { Enabled = true, SecretKey = "" });
            Assert.False(sut.IsConfigured);
        }

        [Fact]
        public void IsConfigured_TrueWhenEnabledAndSecretKeyPresent()
        {
            var sut = BuildService(new StripeOptions { Enabled = true, SecretKey = "sk_test_x" });
            Assert.True(sut.IsConfigured);
        }

        [Fact]
        public async Task CreateCheckoutSessionAsync_NotConfigured_ReturnsAFriendlyErrorInsteadOfCallingStripe()
        {
            var sut = BuildService(new StripeOptions { Enabled = false });

            var result = await sut.CreateCheckoutSessionAsync(OneItemCart(), "a@example.com", "https://x/success", "https://x/cancel");

            Assert.False(result.Success);
            Assert.Contains("Stripe isn't configured", result.Error);
        }

        [Fact]
        public async Task CreateCheckoutSessionAsync_Success_ReturnsUrlAndSessionId()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"cs_test_123\",\"url\":\"https://checkout.stripe.com/pay/cs_test_123\"}")
            });
            var sut = BuildService(new StripeOptions { Enabled = true, SecretKey = "sk_test_x" }, handler);

            var result = await sut.CreateCheckoutSessionAsync(OneItemCart(), "a@example.com", "https://x/success", "https://x/cancel");

            Assert.True(result.Success);
            Assert.Equal("cs_test_123", result.SessionId);
            Assert.Equal("https://checkout.stripe.com/pay/cs_test_123", result.Url);
        }

        [Fact]
        public async Task CreateCheckoutSessionAsync_HttpError_ReturnsAFriendlyErrorWithoutLeakingStripesRawBody()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":{\"message\":\"No such price\"}}")
            });
            var sut = BuildService(new StripeOptions { Enabled = true, SecretKey = "sk_test_x" }, handler);

            var result = await sut.CreateCheckoutSessionAsync(OneItemCart(), "a@example.com", "https://x/success", "https://x/cancel");

            Assert.False(result.Success);
            Assert.DoesNotContain("No such price", result.Error);
        }

        [Fact]
        public async Task GetSessionStatusAsync_ParsesAPaidSession()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"payment_status\":\"paid\",\"amount_total\":1999,\"customer_details\":{\"email\":\"a@example.com\"}}")
            });
            var sut = BuildService(new StripeOptions { Enabled = true, SecretKey = "sk_test_x" }, handler);

            var status = await sut.GetSessionStatusAsync("cs_test_123");

            Assert.True(status.Found);
            Assert.True(status.IsPaid);
            Assert.Equal(19.99m, status.AmountTotal);
            Assert.Equal("a@example.com", status.CustomerEmail);
        }

        [Fact]
        public async Task GetSessionStatusAsync_ParsesAnUnpaidSession_AsNotPaid()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"payment_status\":\"unpaid\",\"amount_total\":1999}")
            });
            var sut = BuildService(new StripeOptions { Enabled = true, SecretKey = "sk_test_x" }, handler);

            var status = await sut.GetSessionStatusAsync("cs_test_123");

            Assert.True(status.Found);
            Assert.False(status.IsPaid);
        }

        [Fact]
        public async Task GetSessionStatusAsync_NotConfigured_ReturnsNotFoundWithoutCallingStripe()
        {
            var sut = BuildService(new StripeOptions { Enabled = false });

            var status = await sut.GetSessionStatusAsync("cs_test_123");

            Assert.False(status.Found);
        }
    }
}

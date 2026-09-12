using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;
using OnlineComputerStore.Core.Services;
using OnlineComputerStore.Tests.TestHelpers;
using Xunit;

namespace OnlineComputerStore.Tests
{
    public class OrderServiceTests : DatabaseTestBase
    {
        private readonly OrderService _sut;
        private readonly FakeEmailService _email = new();

        public OrderServiceTests()
        {
            _sut = new OrderService(Context, _email);
        }

        private Product SeedProduct(decimal price = 100m)
        {
            var product = new Product
            {
                Name = "Test Widget",
                Brand = "TestBrand",
                ShortSpecs = "spec",
                Description = "desc",
                Price = price,
                ImageUrl = "/images/test.jpg",
                Category = "Accessories"
            };
            Context.Products.Add(product);
            Context.SaveChanges();
            return product;
        }

        private static Cart CartFor(Product product, int quantity) => new()
        {
            Items = { new CartItem { ProductId = product.Id, Product = product, Quantity = quantity } }
        };

        [Fact]
        public async Task PlaceOrderAsync_CreatesOrder_WithCorrectTotalAndItems()
        {
            var product = SeedProduct(199.99m);
            var cart = CartFor(product, 2);
            var details = new CheckoutViewModel { CustomerName = "Jane Doe", Email = "jane@example.com", Address = "1 Test St" };

            var order = await _sut.PlaceOrderAsync(details, cart);

            Assert.Equal("Jane Doe", order.CustomerName);
            Assert.Equal(399.98m, order.Total);
            Assert.Single(order.Items);
            Assert.Equal(2, order.Items[0].Quantity);
        }

        [Fact]
        public async Task PlaceOrderAsync_DefaultsToNoPaymentRequired_WhenPaymentNotProvided()
        {
            var product = SeedProduct();
            var order = await _sut.PlaceOrderAsync(
                new CheckoutViewModel { CustomerName = "A", Email = "a@example.com", Address = "x" },
                CartFor(product, 1));

            Assert.Equal("None", order.PaymentProvider);
            Assert.Equal("Not Required", order.PaymentStatus);
            Assert.Null(order.PaymentReference);
        }

        [Fact]
        public async Task PlaceOrderAsync_RecordsStripePaymentFields_WhenProvided()
        {
            var product = SeedProduct();
            var order = await _sut.PlaceOrderAsync(
                new CheckoutViewModel { CustomerName = "A", Email = "a@example.com", Address = "x" },
                CartFor(product, 1),
                paymentProvider: "Stripe",
                paymentStatus: "Paid",
                paymentReference: "cs_test_123");

            Assert.Equal("Stripe", order.PaymentProvider);
            Assert.Equal("Paid", order.PaymentStatus);
            Assert.Equal("cs_test_123", order.PaymentReference);
        }

        [Fact]
        public async Task PlaceOrderAsync_GeneratesAFiveDigitOrderId()
        {
            var product = SeedProduct();
            var order = await _sut.PlaceOrderAsync(
                new CheckoutViewModel { CustomerName = "A", Email = "a@example.com", Address = "x" },
                CartFor(product, 1));

            Assert.InRange(order.Id, 10000, 99999);
        }

        [Fact]
        public async Task GetByPaymentReferenceAsync_FindsExistingOrder_SoASecondCheckoutCantDuplicateIt()
        {
            var product = SeedProduct();
            var placed = await _sut.PlaceOrderAsync(
                new CheckoutViewModel { CustomerName = "A", Email = "a@example.com", Address = "x" },
                CartFor(product, 1),
                paymentProvider: "Stripe",
                paymentStatus: "Paid",
                paymentReference: "cs_test_dup");

            var found = await _sut.GetByPaymentReferenceAsync("cs_test_dup");

            Assert.NotNull(found);
            Assert.Equal(placed.Id, found!.Id);
        }

        [Fact]
        public async Task GetByPaymentReferenceAsync_ReturnsNull_WhenNoOrderMatches()
        {
            var found = await _sut.GetByPaymentReferenceAsync("cs_never_created");
            Assert.Null(found);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_ForAnUnknownId()
        {
            var found = await _sut.GetByIdAsync(1);
            Assert.Null(found);
        }

        [Fact]
        public async Task PlaceOrderAsync_SendsLowStockAlert_WhenAnOrderCrossesTheThreshold()
        {
            var product = SeedProduct();
            product.StockQuantity = 6; // an order of 3 drops this to 3, crossing the <=5 threshold
            Context.SaveChanges();

            await _sut.PlaceOrderAsync(
                new CheckoutViewModel { CustomerName = "A", Email = "a@example.com", Address = "x" },
                CartFor(product, 3));

            Assert.Single(_email.LastLowStockAlert);
            Assert.Equal(product.Id, _email.LastLowStockAlert[0].Id);
        }

        [Fact]
        public async Task PlaceOrderAsync_DoesNotSendLowStockAlert_WhenAlreadyBelowThreshold()
        {
            // Alerting only on the transition into low stock avoids emailing once per
            // sale for an item that's already low — so a second order that keeps it
            // low shouldn't fire a second alert.
            var product = SeedProduct();
            product.StockQuantity = 3;
            Context.SaveChanges();

            await _sut.PlaceOrderAsync(
                new CheckoutViewModel { CustomerName = "A", Email = "a@example.com", Address = "x" },
                CartFor(product, 1));

            Assert.Empty(_email.LastLowStockAlert);
        }
    }
}

using Microsoft.AspNetCore.Http;
using OnlineComputerStore.Core.Models;
using OnlineComputerStore.Core.Services;
using OnlineComputerStore.Tests.TestHelpers;
using Xunit;

namespace OnlineComputerStore.Tests
{
    public class CartServiceTests : DatabaseTestBase
    {
        private readonly CartService _sut;

        public CartServiceTests()
        {
            var httpContext = new DefaultHttpContext { Session = new FakeSession() };
            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            _sut = new CartService(accessor, Context);
        }

        private Product SeedProduct(int id, decimal price)
        {
            var product = new Product
            {
                Id = id,
                Name = $"Product {id}",
                Brand = "Brand",
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

        [Fact]
        public void Add_NewProduct_AddsToCartWithGivenQuantity()
        {
            SeedProduct(1, 50m);

            _sut.Add(1, 3);
            var cart = _sut.GetCart();

            Assert.Single(cart.Items);
            Assert.Equal(3, cart.Items[0].Quantity);
            Assert.Equal(150m, cart.Total);
        }

        [Fact]
        public void Add_SameProductTwice_IncrementsQuantityInsteadOfDuplicatingTheLine()
        {
            SeedProduct(1, 50m);

            _sut.Add(1, 1);
            _sut.Add(1, 2);
            var cart = _sut.GetCart();

            Assert.Single(cart.Items);
            Assert.Equal(3, cart.Items[0].Quantity);
        }

        [Fact]
        public void Add_UnknownProductId_DoesNothing()
        {
            _sut.Add(999, 1);
            Assert.Empty(_sut.GetCart().Items);
        }

        [Fact]
        public void Remove_TakesTheLineOutOfTheCart()
        {
            SeedProduct(1, 50m);
            _sut.Add(1, 1);

            _sut.Remove(1);

            Assert.Empty(_sut.GetCart().Items);
        }

        [Fact]
        public void UpdateQuantity_ZeroOrLess_RemovesTheLine()
        {
            SeedProduct(1, 50m);
            _sut.Add(1, 2);

            _sut.UpdateQuantity(1, 0);

            Assert.Empty(_sut.GetCart().Items);
        }

        [Fact]
        public void UpdateQuantity_PositiveValue_UpdatesTheQuantity()
        {
            SeedProduct(1, 50m);
            _sut.Add(1, 2);

            _sut.UpdateQuantity(1, 5);

            Assert.Equal(5, _sut.GetCart().Items[0].Quantity);
        }

        [Fact]
        public void Clear_EmptiesTheCart()
        {
            SeedProduct(1, 50m);
            _sut.Add(1, 2);

            _sut.Clear();

            Assert.Empty(_sut.GetCart().Items);
        }

        [Fact]
        public void GetCart_SkipsLinesForProductsThatNoLongerExist()
        {
            // A product could be removed from the catalog after a customer added it to
            // their cart in an earlier session; the cart should quietly drop that line
            // rather than crash on the missing product.
            SeedProduct(1, 50m);
            _sut.Add(1, 1);
            Context.Products.Remove(Context.Products.Find(1)!);
            Context.SaveChanges();

            var cart = _sut.GetCart();

            Assert.Empty(cart.Items);
        }
    }
}

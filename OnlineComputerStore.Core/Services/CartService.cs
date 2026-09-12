using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    // Two carts, one interface. A guest's cart lives only in that browser's
    // session (as before). A logged-in user's cart is read from and written to
    // the SavedCartItems table instead, so it survives closing the browser and
    // logging back in later — the whole point of this class being user-aware
    // rather than purely session-based.
    public class CartService : ICartService
    {
        private const string CartKey = "Cart";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly StoreDbContext _context;

        public CartService(IHttpContextAccessor httpContextAccessor, StoreDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        private int? CurrentUserId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity == null || !user.Identity.IsAuthenticated) return null;

                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(idClaim, out var id) ? id : null;
            }
        }

        public Cart GetCart()
        {
            var userId = CurrentUserId;
            return userId.HasValue ? GetAccountCart(userId.Value) : GetSessionCart();
        }

        public void Add(int productId, int quantity = 1)
        {
            var userId = CurrentUserId;
            if (userId.HasValue)
            {
                var product = _context.Products.Find(productId);
                if (product == null || product.StockQuantity <= 0) return;

                var existing = _context.SavedCartItems.FirstOrDefault(s => s.UserId == userId.Value && s.ProductId == productId);
                var currentQty = existing?.Quantity ?? 0;
                // Never let the cart hold more of an item than is actually in stock.
                var newQty = Math.Min(currentQty + quantity, product.StockQuantity);
                if (newQty <= currentQty) return;

                if (existing == null)
                    _context.SavedCartItems.Add(new SavedCartItem { UserId = userId.Value, ProductId = productId, Quantity = newQty });
                else
                    existing.Quantity = newQty;

                _context.SaveChanges();
                return;
            }

            var cart = GetSessionCart();
            var product2 = _context.Products.Find(productId);
            if (product2 == null || product2.StockQuantity <= 0) return;

            var existingLine = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            var currentQty2 = existingLine?.Quantity ?? 0;
            var newQty2 = Math.Min(currentQty2 + quantity, product2.StockQuantity);
            if (newQty2 <= currentQty2) return;

            if (existingLine == null)
                cart.Items.Add(new CartItem { ProductId = productId, Product = product2, Quantity = newQty2 });
            else
                existingLine.Quantity = newQty2;

            SaveSessionCart(cart);
        }

        public void Remove(int productId)
        {
            var userId = CurrentUserId;
            if (userId.HasValue)
            {
                var existing = _context.SavedCartItems.FirstOrDefault(s => s.UserId == userId.Value && s.ProductId == productId);
                if (existing == null) return;

                _context.SavedCartItems.Remove(existing);
                _context.SaveChanges();
                return;
            }

            var cart = GetSessionCart();
            cart.Items.RemoveAll(i => i.ProductId == productId);
            SaveSessionCart(cart);
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var userId = CurrentUserId;
            if (userId.HasValue)
            {
                var existing = _context.SavedCartItems.FirstOrDefault(s => s.UserId == userId.Value && s.ProductId == productId);
                if (existing == null) return;

                if (quantity <= 0)
                    _context.SavedCartItems.Remove(existing);
                else
                    existing.Quantity = quantity;

                _context.SaveChanges();
                return;
            }

            var cart = GetSessionCart();
            var line = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (line == null) return;

            if (quantity <= 0)
                cart.Items.Remove(line);
            else
                line.Quantity = quantity;

            SaveSessionCart(cart);
        }

        public void Clear()
        {
            var userId = CurrentUserId;
            if (userId.HasValue)
            {
                var items = _context.SavedCartItems.Where(s => s.UserId == userId.Value);
                _context.SavedCartItems.RemoveRange(items);
                _context.SaveChanges();
                return;
            }

            Session.Remove(CartKey);
        }

        public void MergeGuestCartIntoAccount(int userId)
        {
            var json = Session.GetString(CartKey);
            if (string.IsNullOrEmpty(json)) return;

            var stored = JsonSerializer.Deserialize<StoredCart>(json) ?? new StoredCart();
            if (stored.Lines.Count == 0) return;

            foreach (var line in stored.Lines)
            {
                var existing = _context.SavedCartItems.FirstOrDefault(s => s.UserId == userId && s.ProductId == line.ProductId);
                if (existing == null)
                {
                    _context.SavedCartItems.Add(new SavedCartItem { UserId = userId, ProductId = line.ProductId, Quantity = line.Quantity });
                }
                else if (line.Quantity > existing.Quantity)
                {
                    // Take the larger quantity rather than adding them together —
                    // right after a logout, the session is a straight copy of the
                    // saved cart, so a naive add would double every item's
                    // quantity on every logout/login round-trip.
                    existing.Quantity = line.Quantity;
                }
            }

            _context.SaveChanges();
            Session.Remove(CartKey);
        }

        public void SnapshotAccountCartToSession(int userId)
        {
            var items = _context.SavedCartItems.Where(s => s.UserId == userId).ToList();
            var stored = new StoredCart
            {
                Lines = items.Select(i => new StoredLine { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
            };
            Session.SetString(CartKey, JsonSerializer.Serialize(stored));
        }

        private Cart GetAccountCart(int userId)
        {
            var cart = new Cart();
            var items = _context.SavedCartItems.Where(s => s.UserId == userId).ToList();
            foreach (var item in items)
            {
                var product = _context.Products.Find(item.ProductId);
                if (product == null) continue;
                cart.Items.Add(new CartItem { ProductId = item.ProductId, Product = product, Quantity = item.Quantity });
            }
            return cart;
        }

        private Cart GetSessionCart()
        {
            var json = Session.GetString(CartKey);
            if (string.IsNullOrEmpty(json)) return new Cart();

            var stored = JsonSerializer.Deserialize<StoredCart>(json) ?? new StoredCart();
            var cart = new Cart();
            foreach (var line in stored.Lines)
            {
                var product = _context.Products.Find(line.ProductId);
                if (product == null) continue;
                cart.Items.Add(new CartItem { ProductId = line.ProductId, Product = product, Quantity = line.Quantity });
            }
            return cart;
        }

        private void SaveSessionCart(Cart cart)
        {
            var stored = new StoredCart
            {
                Lines = cart.Items.Select(i => new StoredLine { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
            };
            Session.SetString(CartKey, JsonSerializer.Serialize(stored));
        }

        private class StoredCart
        {
            public System.Collections.Generic.List<StoredLine> Lines { get; set; } = new();
        }

        private class StoredLine
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly StoreDbContext _context;
        public WishlistService(StoreDbContext context) => _context = context;

        public async Task<List<Product>> GetProductsAsync(int userId)
        {
            var productIds = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();

            return await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        }

        public async Task<HashSet<int>> GetProductIdsAsync(int userId)
        {
            var ids = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();

            return ids.ToHashSet();
        }

        public async Task<bool> ToggleAsync(int userId, int productId)
        {
            var existing = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existing != null)
            {
                _context.WishlistItems.Remove(existing);
                await _context.SaveChangesAsync();
                return false;
            }

            _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ClearAsync(int userId)
        {
            var items = _context.WishlistItems.Where(w => w.UserId == userId);
            _context.WishlistItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}

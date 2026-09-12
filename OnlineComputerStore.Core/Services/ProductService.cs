using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public class ProductService : IProductService
    {
        private readonly StoreDbContext _context;
        public ProductService(StoreDbContext context) => _context = context;

        public async Task<IEnumerable<Product>> GetFeaturedAsync() =>
            await _context.Products.Where(p => p.IsFeatured).ToListAsync();

        public async Task<IEnumerable<Product>> GetAllAsync() =>
            await _context.Products.ToListAsync();

        public async Task<IEnumerable<Product>> SearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return await GetAllAsync();

            var all = await _context.Products.ToListAsync();
            return all.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Brand.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.ShortSpecs.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Product?> GetByIdAsync(int id) =>
            await _context.Products.FindAsync(id);

        public async Task<List<Product>> GetTrendingAsync()
        {
            var since = DateTime.UtcNow.AddDays(-7);

            // Units sold per product across orders placed in the last 7 days.
            var recentQuantities = await (
                from oi in _context.OrderItems
                join o in _context.Orders on oi.OrderId equals o.Id
                where o.CreatedAt >= since
                group oi by oi.ProductId into g
                select new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) }
            ).ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

            var products = await _context.Products.ToListAsync();

            // Products with no recent sales (quantity 0) still get a stable spot
            // via the fallback ordering below, so the shelf isn't empty before a
            // week of real order history has built up.
            return products
                .OrderByDescending(p => recentQuantities.TryGetValue(p.Id, out var q) ? q : 0)
                .ThenByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.StockQuantity > 0)
                .ThenBy(p => p.Id)
                .ToList();
        }
    }
}

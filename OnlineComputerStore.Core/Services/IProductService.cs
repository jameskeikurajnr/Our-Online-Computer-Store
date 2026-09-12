using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetFeaturedAsync();
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> SearchAsync(string term);
        Task<Product?> GetByIdAsync(int id);

        // Every product, ranked by units sold in the last 7 days (ties broken by
        // featured status, then in-stock, then id) — a stable full ranking so
        // callers can take non-overlapping slices, e.g. Home shows the top 2 and
        // Shop shows the next 5, with no product appearing in both places.
        Task<List<Product>> GetTrendingAsync();
    }
}

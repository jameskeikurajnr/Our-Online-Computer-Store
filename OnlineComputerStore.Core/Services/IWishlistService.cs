using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface IWishlistService
    {
        Task<List<Product>> GetProductsAsync(int userId);
        Task<HashSet<int>> GetProductIdsAsync(int userId);

        // Adds the product if it isn't already saved, removes it if it is.
        // Returns true if the product is now in the wishlist, false if it was removed.
        Task<bool> ToggleAsync(int userId, int productId);

        // Removes every saved item for this user.
        Task ClearAsync(int userId);
    }
}

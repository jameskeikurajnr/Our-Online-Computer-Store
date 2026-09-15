using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface IProductRequestService
    {
        // Called whenever a site search returns zero results. Logs the term (or
        // bumps the counter on an existing unresolved entry for the same term)
        // and emails admin — but only the first time a given term is seen, so
        // ten people searching "razer huntsman" in one afternoon sends one email,
        // not ten.
        Task LogAsync(string searchTerm);

        Task<List<ProductRequest>> GetActiveAsync();

        // Marks one request handled (sourced/added to the catalog) — removed
        // from the active list but not deleted, so there's still a record it
        // was asked for and dealt with. Returns false if no such request exists.
        Task<bool> MarkResolvedAsync(int id);

        // Deletes every request outright (unlike MarkResolvedAsync, nothing is kept).
        // Returns how many were removed, for the admin audit log entry.
        Task<int> ClearAllAsync();
    }
}

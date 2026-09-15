using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public class ProductRequestService : IProductRequestService
    {
        private readonly StoreDbContext _context;
        private readonly IEmailService _email;

        public ProductRequestService(StoreDbContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

        public async Task LogAsync(string searchTerm)
        {
            var term = searchTerm?.Trim() ?? "";
            if (term.Length < 3) return;

            // Case-insensitive match against any existing, still-unresolved
            // request for the same term — EF translates ToLower() to SQLite's
            // LOWER(), so this doesn't need a stored-lowercase column.
            var existing = await _context.ProductRequests
                .Where(r => !r.Resolved && r.SearchTerm.ToLower() == term.ToLower())
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.TimesSearched++;
                existing.LastSearchedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Only alert once a term clears 3 searches — the moment it crosses
                // from "maybe a typo" to "people actually keep looking for this".
                // Fired exactly once, on the 3nd search, not on every miss after that.
                if (existing.TimesSearched == 3)
                    await _email.SendProductRequestAlertAsync(term, existing.TimesSearched);

                return;
            }

            _context.ProductRequests.Add(new ProductRequest
            {
                SearchTerm = term,
                TimesSearched = 1,
                FirstSearchedAt = DateTime.UtcNow,
                LastSearchedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // No email on a first-ever search — we wait to see if it's a genuine
            // pattern (more than 2 search) before bothering the admin.
        }

        public async Task<List<ProductRequest>> GetActiveAsync() =>
            await _context.ProductRequests
                .Where(r => !r.Resolved)
                .OrderByDescending(r => r.LastSearchedAt)
                .ToListAsync();

        public async Task<bool> MarkResolvedAsync(int id)
        {
            var request = await _context.ProductRequests.FindAsync(id);
            if (request == null) return false;

            request.Resolved = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> ClearAllAsync()
        {
            var active = await _context.ProductRequests.Where(r => !r.Resolved).ToListAsync();
            _context.ProductRequests.RemoveRange(active);
            await _context.SaveChangesAsync();
            return active.Count;
        }
    }
}

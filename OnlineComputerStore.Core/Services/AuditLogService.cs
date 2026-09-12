using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly StoreDbContext _context;
        public AuditLogService(StoreDbContext context) => _context = context;

        public async Task LogAsync(int adminUserId, string adminName, string action, string details)
        {
            _context.AuditLogEntries.Add(new AuditLogEntry
            {
                AdminUserId = adminUserId,
                AdminName = adminName,
                Action = action,
                Details = details
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLogEntry>> GetRecentAsync(int count = 100) =>
            await _context.AuditLogEntries
                .OrderByDescending(e => e.CreatedAt)
                .Take(count)
                .ToListAsync();

        public async Task ClearAllAsync(int adminUserId, string adminName)
        {
            _context.AuditLogEntries.RemoveRange(_context.AuditLogEntries);
            await _context.SaveChangesAsync();

            // One fresh entry recording the clear itself, so there's still a trace
            // that the log was wiped and by whom — an audit log that can erase its
            // own history without a trace would defeat the point of having one.
            await LogAsync(adminUserId, adminName, "Cleared audit log", "All previous entries were removed.");
        }
    }
}

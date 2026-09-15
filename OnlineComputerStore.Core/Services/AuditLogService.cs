using System;
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

        public async Task ClearOlderThanAsync(int days, int adminUserId, string adminName)
        {
            var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(days));

            var toRemove = _context.AuditLogEntries.Where(e => e.CreatedAt < cutoff);
            _context.AuditLogEntries.RemoveRange(toRemove);
            await _context.SaveChangesAsync();

            await LogAsync(adminUserId, adminName, "Cleared audit log entries by age",
                $"Removed entries older than {days} day(s) (before {cutoff:d MMM yyyy}).");
        }

        public async Task ClearByMonthAsync(int year, int month, int adminUserId, string adminName)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);

            var toRemove = _context.AuditLogEntries.Where(e => e.CreatedAt >= start && e.CreatedAt < end);
            _context.AuditLogEntries.RemoveRange(toRemove);
            await _context.SaveChangesAsync();

            await LogAsync(adminUserId, adminName, "Cleared audit log entries by month",
                $"Removed entries from {start:MMMM yyyy}.");
        }
    }
}

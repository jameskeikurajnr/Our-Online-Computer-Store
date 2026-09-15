using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(int adminUserId, string adminName, string action, string details);
        Task<List<AuditLogEntry>> GetRecentAsync(int count = 100);

        // Wipes every existing entry, then writes one fresh entry recording the wipe
        // itself — so the log can be reset without losing the trace that it was.
        Task ClearAllAsync(int adminUserId, string adminName);

        // Removes entries older than the given number of days — a rolling retention
        // clear rather than wiping everything.
        Task ClearOlderThanAsync(int days, int adminUserId, string adminName);

        // Removes entries that fall within one specific calendar month/year.
        Task ClearByMonthAsync(int year, int month, int adminUserId, string adminName);
    }
}

using System;

namespace OnlineComputerStore.Core.Models
{
    public class AuditLogEntry
    {
        public int Id { get; set; }
        public int AdminUserId { get; set; }
        // Snapshot of the admin's name at the time of the action, so the log stays
        // readable even if that account is later renamed or deleted.
        public string AdminName { get; set; } = "";
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

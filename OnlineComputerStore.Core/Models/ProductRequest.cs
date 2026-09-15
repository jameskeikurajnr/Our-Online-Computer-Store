using System;

namespace OnlineComputerStore.Core.Models
{
    // Logged whenever a site search comes back with zero matches — the store's
    // own record of "a customer wanted this and we don't carry it," so admin can
    // go source it and add it to the catalog rather than relying on anyone
    // remembering to check search analytics. Repeated searches for the same term
    // bump TimesSearched instead of creating duplicate rows or re-sending the
    // alert email every time.
    public class ProductRequest
    {
        public int Id { get; set; }
        public string SearchTerm { get; set; } = "";
        public int TimesSearched { get; set; } = 1;
        public DateTime FirstSearchedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSearchedAt { get; set; } = DateTime.UtcNow;

        // Set once admin has sourced it (and ideally added it to the catalog) —
        // resolved requests drop off the active list but aren't deleted, so
        // there's still a record of what was asked for and handled.
        public bool Resolved { get; set; }
    }
}

namespace OnlineComputerStore.Core.Models
{
    // A logged-in user's cart, persisted server-side so it survives closing the
    // browser entirely and logging back in later — unlike the guest cart, which
    // only lives in that browser's session. One row per (user, product); the
    // unique index in StoreDbContext keeps it that way.
    public class SavedCartItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}

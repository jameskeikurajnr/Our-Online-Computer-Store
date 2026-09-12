using System;

namespace OnlineComputerStore.Core.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        // Snapshot of the reviewer's name at the time of posting, same pattern as
        // OrderItem.ProductName — keeps the review readable even if the account is
        // later renamed or deleted.
        public string UserName { get; set; } = "";
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

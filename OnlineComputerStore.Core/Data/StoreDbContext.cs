using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Data
{
    public class StoreDbContext : DbContext
    {
        public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<SavedCartItem> SavedCartItems => Set<SavedCartItem>();
        public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
        public DbSet<ProductRequest> ProductRequests => Set<ProductRequest>();
        public DbSet<PageView> PageViews => Set<PageView>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId);

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<SavedCartItem>()
                .HasIndex(s => new { s.UserId, s.ProductId })
                .IsUnique();

            modelBuilder.Entity<ProductReview>()
                .HasIndex(r => new { r.ProductId, r.UserId })
                .IsUnique();

            modelBuilder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique();

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public class ReviewService : IReviewService
    {
        private readonly StoreDbContext _context;
        public ReviewService(StoreDbContext context) => _context = context;

        public async Task<List<ProductReview>> GetForProductAsync(int productId) =>
            await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<(double Average, int Count)> GetRatingSummaryAsync(int productId)
        {
            var ratings = await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .Select(r => r.Rating)
                .ToListAsync();

            return ratings.Count == 0 ? (0, 0) : (ratings.Average(), ratings.Count);
        }

        public async Task<bool> HasReviewedAsync(int productId, int userId) =>
            await _context.ProductReviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId);

        public async Task<(bool Success, string Message)> AddReviewAsync(int productId, int userId, string userName, int rating, string comment)
        {
            if (rating < 1 || rating > 5)
                return (false, "Rating must be between 1 and 5.");

            if (string.IsNullOrWhiteSpace(comment))
                return (false, "Please write a short comment with your review. Thank you");

            // One review per customer per product — matched by the unique DB index too.
            if (await HasReviewedAsync(productId, userId))
                return (false, "You've already reviewed this product. God Bless you");

            _context.ProductReviews.Add(new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                UserName = userName,
                Rating = rating,
                Comment = comment.Trim()
            });
            await _context.SaveChangesAsync();
            return (true, "Review submitted.");
        }
    }
}

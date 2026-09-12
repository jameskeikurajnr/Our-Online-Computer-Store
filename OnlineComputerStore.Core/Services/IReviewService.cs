using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface IReviewService
    {
        Task<List<ProductReview>> GetForProductAsync(int productId);
        Task<(double Average, int Count)> GetRatingSummaryAsync(int productId);
        Task<bool> HasReviewedAsync(int productId, int userId);
        Task<(bool Success, string Message)> AddReviewAsync(int productId, int userId, string userName, int rating, string comment);
    }
}

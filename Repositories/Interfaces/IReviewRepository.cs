using BusinessObjects.Reviews;
using DTOs.Reviews;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review, Guid>
    {
        Task<IEnumerable<Review>> GetReviewsByProductIdAsync(Guid productId);
        Task<IEnumerable<Review>> GetReviewsByUserIdAsync(Guid userId);
        Task<Review?> GetReviewByOrderAndProductAsync(Guid orderId, Guid productId);
        Task<ReviewStatsDto> GetProductReviewStatsAsync(Guid productId);
        Task<IEnumerable<Review>> GetPendingReviewsAsync();
        Task<bool> HasUserReviewedProductAsync(Guid userId, Guid productId);
        Task<bool> IsVerifiedPurchaseAsync(Guid userId, Guid productId);
        Task UpdateHelpfulCountAsync(Guid reviewId, bool isHelpful);
    }
}
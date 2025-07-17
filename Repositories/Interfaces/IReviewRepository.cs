using BusinessObjects.Reviews;
using DTOs.Reviews;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review, Guid>
    {
        Task<PagedList<Review>> GetReviewsAsync(ReviewFilterDto filter);
        Task<Review?> GetReviewDetailsAsync(Guid reviewId);
        Task<bool> HasUserReviewedVariantInOrderAsync(Guid userId, Guid productVariantId, Guid orderId);
        Task<ReviewStatsDto> GetReviewStatsByVariantIdAsync(Guid productVariantId);
        Task<IEnumerable<Review>> GetApprovedReviewsByProductIdAsync(Guid productId);
        
    }
}
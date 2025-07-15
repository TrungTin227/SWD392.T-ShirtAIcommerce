using BusinessObjects.Reviews;
using DTOs.Reviews;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class ReviewRepository : GenericRepository<Review, Guid>, IReviewRepository
    {
        public ReviewRepository(T_ShirtAIcommerceContext context) : base(context) { }

        public async Task<PagedList<Review>> GetReviewsAsync(ReviewFilterDto filter)
        {
            var query = _dbSet.AsQueryable();

            if (filter.ProductVariantId.HasValue)
            {
                query = query.Where(r => r.ProductVariantId == filter.ProductVariantId.Value);
            }
            if (filter.UserId.HasValue)
            {
                query = query.Where(r => r.UserId == filter.UserId.Value);
            }
            if (filter.Rating.HasValue)
            {
                query = query.Where(r => r.Rating == filter.Rating.Value);
            }
            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }

            // Include related data for DTO mapping
            query = query
                .Include(r => r.User)
                .Include(r => r.ProductVariant)
                    .ThenInclude(pv => pv.Product);

            // Sorting
            query = filter.OrderBy?.ToLower() switch
            {
                "rating" => filter.OrderByDescending ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
                _ => filter.OrderByDescending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
            };

            return await PagedList<Review>.ToPagedListAsync(query, filter.PageNumber, filter.PageSize);
        }

        public async Task<Review?> GetReviewDetailsAsync(Guid reviewId)
        {
            return await _dbSet
                .Include(r => r.User)
                .Include(r => r.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted);
        }

        public async Task<bool> HasUserReviewedVariantInOrderAsync(Guid userId, Guid productVariantId, Guid orderId)
        {
            return await _dbSet.AnyAsync(r => r.UserId == userId
                                           && r.ProductVariantId == productVariantId
                                           && r.OrderId == orderId
                                           && !r.IsDeleted);
        }

        public async Task<ReviewStatsDto> GetReviewStatsByVariantIdAsync(Guid productVariantId)
        {
            var query = _dbSet.Where(r => r.ProductVariantId == productVariantId && r.Status == ReviewStatus.Approved && !r.IsDeleted);

            var totalReviews = await query.CountAsync();
            if (totalReviews == 0)
            {
                return new ReviewStatsDto(); // Return default stats
            }

            var stats = new ReviewStatsDto
            {
                TotalReviews = totalReviews,
                AverageRating = await query.AverageAsync(r => r.Rating),
                RatingDistribution = await query
                    .GroupBy(r => r.Rating)
                    .ToDictionaryAsync(g => g.Key, g => g.Count())
            };

            // Ensure all rating levels (1-5) are present in the dictionary
            for (int i = 1; i <= 5; i++)
            {
                if (!stats.RatingDistribution.ContainsKey(i))
                {
                    stats.RatingDistribution[i] = 0;
                }
            }

            return stats;
        }
    }
}
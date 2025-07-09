using BusinessObjects.Reviews;
using DTOs.Reviews;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;
using BusinessObjects.Common;

namespace Repositories.Implementations
{
    public class ReviewRepository : GenericRepository<Review, Guid>, IReviewRepository
    {
        public ReviewRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Review>> GetReviewsByProductIdAsync(Guid productId)
        {
            return await _context.Reviews
                .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetReviewsByUserIdAsync(Guid userId)
        {
            return await _context.Reviews
                .Where(r => r.UserId == userId)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetReviewByOrderAndProductAsync(Guid orderId, Guid productId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ProductId == productId);
        }

        public async Task<ReviewStatsDto> GetProductReviewStatsAsync(Guid productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
                .ToListAsync();

            if (!reviews.Any())
            {
                return new ReviewStatsDto();
            }

            var stats = new ReviewStatsDto
            {
                TotalReviews = reviews.Count,
                AverageRating = reviews.Average(r => r.Rating),
                VerifiedPurchasesCount = reviews.Count(r => r.IsVerifiedPurchase),
                RatingDistribution = reviews
                    .GroupBy(r => r.Rating)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Ensure all ratings 1-5 are represented
            for (int i = 1; i <= 5; i++)
            {
                if (!stats.RatingDistribution.ContainsKey(i))
                {
                    stats.RatingDistribution[i] = 0;
                }
            }

            return stats;
        }

        public async Task<IEnumerable<Review>> GetPendingReviewsAsync()
        {
            return await _context.Reviews
                .Where(r => r.Status == ReviewStatus.Pending)
                .Include(r => r.User)
                .Include(r => r.Product)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasUserReviewedProductAsync(Guid userId, Guid productId)
        {
            return await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.ProductId == productId);
        }

        public async Task<bool> IsVerifiedPurchaseAsync(Guid userId, Guid productId)
        {
            return await _context.OrderItems
                .Include(oi => oi.Order)
                .AnyAsync(oi => oi.Order!.UserId == userId && 
                               oi.ProductId == productId && 
                               oi.Order.Status == OrderStatus.Delivered);
        }

        public async Task UpdateHelpfulCountAsync(Guid reviewId, bool isHelpful)
        {
            var review = await GetByIdAsync(reviewId);
            if (review != null)
            {
                if (isHelpful)
                    review.HelpfulCount++;
                else
                    review.UnhelpfulCount++;

                await UpdateAsync(review);
            }
        }
    }
}
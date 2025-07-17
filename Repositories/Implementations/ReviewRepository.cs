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
            // Xây dựng câu truy vấn IQueryable của bạn ở đây
            IQueryable<Review> query = _dbSet.AsQueryable();

            // Áp dụng các bộ lọc (filter) nếu có
            // if (!string.IsNullOrEmpty(filter.SearchTerm)) { ... }
            // if (filter.Rating.HasValue) { ... }

            // Sắp xếp
            query = query.OrderByDescending(r => r.CreatedAt);

            // Trả về kết quả phân trang bằng phương thức mở rộng
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
        public async Task<IEnumerable<Review>> GetApprovedReviewsByProductIdAsync(Guid productId)
        {
            // Chúng ta cần lấy tất cả các review có ProductVariant thuộc về ProductId được cho.
            // Điều kiện: Review phải ở trạng thái "Approved" và chưa bị xóa mềm.
            return await _dbSet
                .Where(r => r.ProductVariant.ProductId == productId &&
                            r.Status == ReviewStatus.Approved &&
                            !r.IsDeleted)
                .Include(r => r.User) // Lấy thông tin người dùng
                .Include(r => r.ProductVariant) // Lấy thông tin biến thể
                    .ThenInclude(pv => pv.Product) // Lấy thông tin sản phẩm
                .OrderByDescending(r => r.CreatedAt) // Sắp xếp mới nhất lên đầu
                .ToListAsync();
        }
    }
}
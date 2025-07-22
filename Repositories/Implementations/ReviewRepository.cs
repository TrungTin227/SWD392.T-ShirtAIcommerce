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
            // 1. Bắt đầu câu truy vấn và bao gồm các dữ liệu liên quan để tránh lỗi N+1
            IQueryable<Review> query = _dbSet
                .Include(r => r.User) // Tải thông tin người dùng
                .Include(r => r.ProductVariant) // Tải thông tin biến thể
                    .ThenInclude(pv => pv.Product) // Từ biến thể, tải thông tin sản phẩm
                .AsQueryable();

            // 2. Áp dụng các bộ lọc (filter) một cách có điều kiện
            // Chỉ thêm mệnh đề WHERE nếu giá trị filter được cung cấp

            // Lọc theo ProductVariantId
            if (filter.ProductVariantId.HasValue && filter.ProductVariantId.Value != Guid.Empty)
            {
                query = query.Where(r => r.ProductVariantId == filter.ProductVariantId.Value);
            }

            // Lọc theo UserId
            if (filter.UserId.HasValue && filter.UserId.Value != Guid.Empty)
            {
                query = query.Where(r => r.UserId == filter.UserId.Value);
            }

            // Lọc theo số sao (Rating)
            if (filter.Rating.HasValue)
            {
                query = query.Where(r => r.Rating == filter.Rating.Value);
            }

            // Lọc theo trạng thái (Status)
            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }

            // 3. Áp dụng sắp xếp động
            // Mặc định sắp xếp theo ngày tạo mới nhất
            if (!string.IsNullOrWhiteSpace(filter.OrderBy))
            {
                switch (filter.OrderBy.ToLowerInvariant())
                {
                    case "rating":
                        query = filter.OrderByDescending
                            ? query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt)
                            : query.OrderBy(r => r.Rating).ThenBy(r => r.CreatedAt);
                        break;
                    case "createdat":
                    default:
                        query = filter.OrderByDescending
                            ? query.OrderByDescending(r => r.CreatedAt)
                            : query.OrderBy(r => r.CreatedAt);
                        break;
                }
            }
            else
            {
                // Sắp xếp mặc định nếu OrderBy không được cung cấp
                query = query.OrderByDescending(r => r.CreatedAt);
            }

            // 4. Thực thi truy vấn với phân trang
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
                            !r.IsDeleted)
                .Include(r => r.User) // Lấy thông tin người dùng
                .Include(r => r.ProductVariant) // Lấy thông tin biến thể
                    .ThenInclude(pv => pv.Product) // Lấy thông tin sản phẩm
                .OrderByDescending(r => r.CreatedAt) // Sắp xếp mới nhất lên đầu
                .ToListAsync();
        }
        public async Task<Review?> GetReviewByVariantAndUserAsync(Guid productVariantId, Guid userId)
        {
            // Tìm bài đánh giá đầu tiên (và duy nhất) khớp với cả hai điều kiện
            // và bao gồm các thông tin liên quan để mapping sang DTO
            return await _dbSet
                .Include(r => r.User)
                .Include(r => r.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(r => r.ProductVariantId == productVariantId &&
                                            r.UserId == userId &&
                                            !r.IsDeleted);
        }

    }
}
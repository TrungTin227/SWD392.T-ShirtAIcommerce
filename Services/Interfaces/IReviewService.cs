using DTOs.Common;
using DTOs.Reviews;
using Repositories.Commons;

namespace Services.Interfaces
{
    public interface IReviewService
    {
        Task<PagedResponse<ReviewDto>> GetReviewsAsync(ReviewFilterDto filter);
        Task<ReviewDto?> GetReviewByIdAsync(Guid reviewId);
        Task<ReviewStatsDto?> GetReviewStatsAsync(Guid productVariantId);
        Task<ReviewDto?> CreateReviewAsync(CreateReviewDto createDto, Guid userId);
        Task<ApiResult<IEnumerable<ReviewDto>>> GetReviewsForProductAsync(Guid productVariantId);

    }
}
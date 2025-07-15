using DTOs.Common;
using DTOs.Reviews;

namespace Services.Interfaces
{
    public interface IReviewService
    {
        Task<PagedResponse<ReviewDto>> GetReviewsAsync(ReviewFilterDto filter);
        Task<ReviewDto?> GetReviewByIdAsync(Guid reviewId);
        Task<ReviewStatsDto?> GetReviewStatsAsync(Guid productVariantId);
        Task<ReviewDto?> CreateReviewAsync(CreateReviewDto createDto, Guid userId);
    }
}
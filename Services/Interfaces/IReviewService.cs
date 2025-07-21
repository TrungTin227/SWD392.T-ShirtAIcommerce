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
        Task<ApiResult<ReviewDto>> UpdateReviewAsync(Guid reviewId, UpdateReviewDto updateDto, Guid userId);
        Task<ApiResult> DeleteReviewAsync(Guid reviewId, Guid userId);

        Task<ReviewDto?> GetMyReviewForVariantAsync(Guid productVariantId, Guid userId);


    }
}
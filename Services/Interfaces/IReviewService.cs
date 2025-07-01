using DTOs.Reviews;
using Repositories.Commons;
using Repositories.Helpers;

namespace Services.Interfaces
{
    public interface IReviewService
    {
        Task<ApiResult<ReviewDto>> GetByIdAsync(Guid id);
        Task<ApiResult<PagedList<ReviewDto>>> GetReviewsAsync(ReviewFilterDto filter);
        Task<ApiResult<List<ReviewDto>>> GetProductReviewsAsync(Guid productId);
        Task<ApiResult<List<ReviewDto>>> GetUserReviewsAsync(Guid userId);
        Task<ApiResult<ReviewDto>> CreateReviewAsync(CreateReviewDto createDto);
        Task<ApiResult<ReviewDto>> UpdateReviewAsync(Guid id, UpdateReviewDto updateDto);
        Task<ApiResult<ReviewDto>> AdminUpdateReviewAsync(Guid id, AdminUpdateReviewDto updateDto);
        Task<ApiResult<bool>> DeleteReviewAsync(Guid id);
        Task<ApiResult<ReviewStatsDto>> GetProductReviewStatsAsync(Guid productId);
        Task<ApiResult<ProductReviewSummaryDto>> GetProductReviewSummaryAsync(Guid productId);
        Task<ApiResult<List<ReviewDto>>> GetPendingReviewsAsync();
        Task<ApiResult<bool>> MarkReviewHelpfulAsync(Guid reviewId, bool isHelpful);
        Task<ApiResult<bool>> CanUserReviewProductAsync(Guid userId, Guid productId);
    }
}
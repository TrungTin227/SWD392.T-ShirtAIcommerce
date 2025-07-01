using BusinessObjects.Reviews;
using DTOs.Reviews;
using Microsoft.EntityFrameworkCore;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Interfaces;
using System.Text.Json;

namespace Services.Implementations
{
    public class ReviewService : BaseService<Review, Guid>, IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IProductRepository _productRepository;

        public ReviewService(
            IReviewRepository reviewRepository,
            IProductRepository productRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(reviewRepository, currentUserService, unitOfWork, currentTime)
        {
            _reviewRepository = reviewRepository;
            _productRepository = productRepository;
        }

        public async Task<ApiResult<ReviewDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(id);
                if (review == null)
                {
                    return ApiResult<ReviewDto>.Failure("Review not found");
                }

                var reviewDto = await MapToReviewDto(review);
                return ApiResult<ReviewDto>.Success(reviewDto);
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewDto>.Failure($"Error retrieving review: {ex.Message}");
            }
        }

        public async Task<ApiResult<PagedList<ReviewDto>>> GetReviewsAsync(ReviewFilterDto filter)
        {
            try
            {
                var query = _reviewRepository.GetQueryable()
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .AsQueryable();

                // Apply filters
                if (filter.ProductId.HasValue)
                    query = query.Where(r => r.ProductId == filter.ProductId);

                if (filter.UserId.HasValue)
                    query = query.Where(r => r.UserId == filter.UserId);

                if (filter.OrderId.HasValue)
                    query = query.Where(r => r.OrderId == filter.OrderId);

                if (filter.Rating.HasValue)
                    query = query.Where(r => r.Rating == filter.Rating);

                if (filter.Status.HasValue)
                    query = query.Where(r => r.Status == filter.Status);

                if (filter.IsVerifiedPurchase.HasValue)
                    query = query.Where(r => r.IsVerifiedPurchase == filter.IsVerifiedPurchase);

                if (filter.FromDate.HasValue)
                    query = query.Where(r => r.CreatedAt >= filter.FromDate);

                if (filter.ToDate.HasValue)
                    query = query.Where(r => r.CreatedAt <= filter.ToDate);

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                    query = query.Where(r => r.Content.Contains(filter.SearchTerm));

                // Apply ordering
                query = filter.OrderBy.ToLower() switch
                {
                    "rating" => filter.OrderByDescending ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
                    "helpful" => filter.OrderByDescending ? query.OrderByDescending(r => r.HelpfulCount) : query.OrderBy(r => r.HelpfulCount),
                    _ => filter.OrderByDescending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt)
                };

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var reviewDtos = new List<ReviewDto>();
                foreach (var review in items)
                {
                    reviewDtos.Add(await MapToReviewDto(review));
                }

                var pagedResult = new PagedList<ReviewDto>(reviewDtos, totalCount, filter.Page, filter.PageSize);
                return ApiResult<PagedList<ReviewDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<ReviewDto>>.Failure($"Error retrieving reviews: {ex.Message}");
            }
        }

        public async Task<ApiResult<List<ReviewDto>>> GetProductReviewsAsync(Guid productId)
        {
            try
            {
                var reviews = await _reviewRepository.GetReviewsByProductIdAsync(productId);
                var reviewDtos = new List<ReviewDto>();
                
                foreach (var review in reviews)
                {
                    reviewDtos.Add(await MapToReviewDto(review));
                }

                return ApiResult<List<ReviewDto>>.Success(reviewDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<List<ReviewDto>>.Failure($"Error retrieving product reviews: {ex.Message}");
            }
        }

        public async Task<ApiResult<List<ReviewDto>>> GetUserReviewsAsync(Guid userId)
        {
            try
            {
                var reviews = await _reviewRepository.GetReviewsByUserIdAsync(userId);
                var reviewDtos = new List<ReviewDto>();
                
                foreach (var review in reviews)
                {
                    reviewDtos.Add(await MapToReviewDto(review));
                }

                return ApiResult<List<ReviewDto>>.Success(reviewDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<List<ReviewDto>>.Failure($"Error retrieving user reviews: {ex.Message}");
            }
        }

        public async Task<ApiResult<ReviewDto>> CreateReviewAsync(CreateReviewDto createDto)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (!currentUserId.HasValue)
                {
                    return ApiResult<ReviewDto>.Failure("User not authenticated");
                }

                // Check if user has already reviewed this product
                var existingReview = await _reviewRepository.HasUserReviewedProductAsync(currentUserId.Value, createDto.ProductId);
                if (existingReview)
                {
                    return ApiResult<ReviewDto>.Failure("You have already reviewed this product");
                }

                // Check if product exists
                var product = await _productRepository.GetByIdAsync(createDto.ProductId);
                if (product == null)
                {
                    return ApiResult<ReviewDto>.Failure("Product not found");
                }

                // Check if this is a verified purchase
                var isVerifiedPurchase = await _reviewRepository.IsVerifiedPurchaseAsync(currentUserId.Value, createDto.ProductId);

                var review = new Review
                {
                    UserId = currentUserId.Value,
                    ProductId = createDto.ProductId,
                    OrderId = createDto.OrderId,
                    Rating = createDto.Rating,
                    Content = createDto.Content,
                    Images = createDto.Images != null ? JsonSerializer.Serialize(createDto.Images) : null,
                    IsVerifiedPurchase = isVerifiedPurchase,
                    Status = ReviewStatus.Pending,
                    CreatedAt = _currentTime.GetVietnamTime(),
                    CreatedBy = currentUserId.Value
                };

                await _reviewRepository.AddAsync(review);
                await _unitOfWork.SaveChangesAsync();

                var reviewDto = await MapToReviewDto(review);
                return ApiResult<ReviewDto>.Success(reviewDto);
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewDto>.Failure($"Error creating review: {ex.Message}");
            }
        }

        public async Task<ApiResult<ReviewDto>> UpdateReviewAsync(Guid id, UpdateReviewDto updateDto)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var review = await _reviewRepository.GetByIdAsync(id);
                
                if (review == null)
                {
                    return ApiResult<ReviewDto>.Failure("Review not found");
                }

                if (!currentUserId.HasValue || review.UserId != currentUserId.Value)
                {
                    return ApiResult<ReviewDto>.Failure("You can only update your own reviews");
                }

                review.Rating = updateDto.Rating;
                review.Content = updateDto.Content;
                review.Images = updateDto.Images != null ? JsonSerializer.Serialize(updateDto.Images) : null;
                review.UpdatedAt = _currentTime.GetVietnamTime();
                review.UpdatedBy = currentUserId.Value;

                await _reviewRepository.UpdateAsync(review);
                await _unitOfWork.SaveChangesAsync();

                var reviewDto = await MapToReviewDto(review);
                return ApiResult<ReviewDto>.Success(reviewDto);
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewDto>.Failure($"Error updating review: {ex.Message}");
            }
        }

        public async Task<ApiResult<ReviewDto>> AdminUpdateReviewAsync(Guid id, AdminUpdateReviewDto updateDto)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var review = await _reviewRepository.GetByIdAsync(id);
                
                if (review == null)
                {
                    return ApiResult<ReviewDto>.Failure("Review not found");
                }

                review.Status = updateDto.Status;
                review.AdminNotes = updateDto.AdminNotes;
                review.UpdatedAt = _currentTime.GetVietnamTime();
                review.UpdatedBy = currentUserId ?? Guid.Empty;

                await _reviewRepository.UpdateAsync(review);
                await _unitOfWork.SaveChangesAsync();

                var reviewDto = await MapToReviewDto(review);
                return ApiResult<ReviewDto>.Success(reviewDto);
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewDto>.Failure($"Error updating review: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> DeleteReviewAsync(Guid id)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var review = await _reviewRepository.GetByIdAsync(id);
                
                if (review == null)
                {
                    return ApiResult<bool>.Failure("Review not found");
                }

                if (!currentUserId.HasValue || review.UserId != currentUserId.Value)
                {
                    return ApiResult<bool>.Failure("You can only delete your own reviews");
                }

                await _reviewRepository.DeleteAsync(review.Id);
                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error deleting review: {ex.Message}");
            }
        }

        public async Task<ApiResult<ReviewStatsDto>> GetProductReviewStatsAsync(Guid productId)
        {
            try
            {
                var stats = await _reviewRepository.GetProductReviewStatsAsync(productId);
                return ApiResult<ReviewStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewStatsDto>.Failure($"Error retrieving review stats: {ex.Message}");
            }
        }

        public async Task<ApiResult<ProductReviewSummaryDto>> GetProductReviewSummaryAsync(Guid productId)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(productId);
                if (product == null)
                {
                    return ApiResult<ProductReviewSummaryDto>.Failure("Product not found");
                }

                var stats = await _reviewRepository.GetProductReviewStatsAsync(productId);
                var recentReviews = await _reviewRepository.GetReviewsByProductIdAsync(productId);
                
                var recentReviewDtos = new List<ReviewDto>();
                foreach (var review in recentReviews.Take(5))
                {
                    recentReviewDtos.Add(await MapToReviewDto(review));
                }

                var summary = new ProductReviewSummaryDto
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    Stats = stats,
                    RecentReviews = recentReviewDtos
                };

                return ApiResult<ProductReviewSummaryDto>.Success(summary);
            }
            catch (Exception ex)
            {
                return ApiResult<ProductReviewSummaryDto>.Failure($"Error retrieving product review summary: {ex.Message}");
            }
        }

        public async Task<ApiResult<List<ReviewDto>>> GetPendingReviewsAsync()
        {
            try
            {
                var reviews = await _reviewRepository.GetPendingReviewsAsync();
                var reviewDtos = new List<ReviewDto>();
                
                foreach (var review in reviews)
                {
                    reviewDtos.Add(await MapToReviewDto(review));
                }

                return ApiResult<List<ReviewDto>>.Success(reviewDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<List<ReviewDto>>.Failure($"Error retrieving pending reviews: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> MarkReviewHelpfulAsync(Guid reviewId, bool isHelpful)
        {
            try
            {
                await _reviewRepository.UpdateHelpfulCountAsync(reviewId, isHelpful);
                await _unitOfWork.SaveChangesAsync();
                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error marking review helpful: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> CanUserReviewProductAsync(Guid userId, Guid productId)
        {
            try
            {
                var hasReviewed = await _reviewRepository.HasUserReviewedProductAsync(userId, productId);
                var canReview = !hasReviewed;
                return ApiResult<bool>.Success(canReview);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error checking review eligibility: {ex.Message}");
            }
        }

        private async Task<ReviewDto> MapToReviewDto(Review review)
        {
            List<string>? images = null;
            if (!string.IsNullOrEmpty(review.Images))
            {
                try
                {
                    images = JsonSerializer.Deserialize<List<string>>(review.Images);
                }
                catch
                {
                    // If deserialization fails, leave images as null
                }
            }

            return new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                UserName = review.User?.UserName ?? "Unknown User",
                ProductId = review.ProductId,
                ProductName = review.Product?.Name,
                OrderId = review.OrderId,
                Rating = review.Rating,
                Content = review.Content,
                Images = images,
                HelpfulCount = review.HelpfulCount,
                UnhelpfulCount = review.UnhelpfulCount,
                Status = review.Status,
                AdminNotes = review.AdminNotes,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };
        }
    }
}
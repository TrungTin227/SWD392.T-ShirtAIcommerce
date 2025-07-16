using BusinessObjects.Common;
using BusinessObjects.Reviews;
using DTOs.Common;
using DTOs.Reviews;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IOrderRepository _orderRepository; 
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(IReviewRepository reviewRepository, IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<ReviewService> logger)
        {
            _reviewRepository = reviewRepository;
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PagedResponse<ReviewDto>> GetReviewsAsync(ReviewFilterDto filter)
        {
            var pagedReviews = await _reviewRepository.GetReviewsAsync(filter);
            var reviewDtos = pagedReviews.Select(MapToDto).ToList();

            var response = new PagedResponse<ReviewDto>
            {
                Errors = new List<string>()
                                             
            };
            return response;
        }

        public async Task<ReviewDto?> GetReviewByIdAsync(Guid reviewId)
        {
            var review = await _reviewRepository.GetReviewDetailsAsync(reviewId);
            return review != null ? MapToDto(review) : null;
        }

        public async Task<ReviewStatsDto?> GetReviewStatsAsync(Guid productVariantId)
        {
            try
            {
                return await _reviewRepository.GetReviewStatsByVariantIdAsync(productVariantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review stats for variant {VariantId}", productVariantId);
                return null;
            }
        }

        public async Task<ReviewDto?> CreateReviewAsync(CreateReviewDto createDto, Guid userId)
        {
            try
            {
                // 1. Kiểm tra xem người dùng có thực sự mua sản phẩm này trong đơn hàng đó không
                var order = await _orderRepository.GetOrderWithDetailsAsync(createDto.OrderId);
                if (order == null || order.UserId != userId || order.Status != OrderStatus.Completed)
                {
                    throw new InvalidOperationException("Bạn chỉ có thể đánh giá sản phẩm từ đơn hàng đã hoàn thành.");
                }

                var purchasedItem = order.OrderItems.FirstOrDefault(oi => oi.ProductVariantId == createDto.ProductVariantId);
                if (purchasedItem == null)
                {
                    throw new InvalidOperationException("Sản phẩm này không có trong đơn hàng của bạn.");
                }

                // 2. Kiểm tra xem người dùng đã đánh giá sản phẩm này cho đơn hàng này chưa
                var hasReviewed = await _reviewRepository.HasUserReviewedVariantInOrderAsync(userId, createDto.ProductVariantId, createDto.OrderId);
                if (hasReviewed)
                {
                    throw new InvalidOperationException("Bạn đã đánh giá sản phẩm này rồi.");
                }

                // 3. Tạo review mới
                var review = new Review
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductVariantId = createDto.ProductVariantId,
                    OrderId = createDto.OrderId,
                    Rating = createDto.Rating,
                    Content = createDto.Content,
                    Status = ReviewStatus.Pending, // Chờ duyệt
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _reviewRepository.AddAsync(review);
                await _unitOfWork.SaveChangesAsync();

                var newReviewDetails = await _reviewRepository.GetReviewDetailsAsync(review.Id);
                return MapToDto(newReviewDetails!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for user {UserId}", userId);
                return null;
            }
        }

        private ReviewDto MapToDto(Review review)
        {
            return new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                UserName = review.User?.UserName ?? "Anonymous",
                ProductVariantId = review.ProductVariantId ?? Guid.Empty, // Fix here
                VariantInfo = $"Màu: {review.ProductVariant?.Color}, Size: {review.ProductVariant?.Size}",
                ProductId = review.ProductVariant?.ProductId ?? Guid.Empty,
                ProductName = review.ProductVariant?.Product?.Name ?? "Unknown Product",
                OrderId = review.OrderId ?? Guid.Empty, // Fix here
                Rating = review.Rating,
                Content = review.Content,
                Images = string.IsNullOrEmpty(review.Images) ? new List<string>() : review.Images.Split(',').ToList(), // Fix here
                Status = review.Status,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
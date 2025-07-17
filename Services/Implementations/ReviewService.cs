using BusinessObjects.Common;
using BusinessObjects.Reviews;
using DTOs.Common;
using DTOs.Reviews;
using Microsoft.Extensions.Logging;
using Repositories.Commons;
using Repositories.Implementations;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IOrderRepository _orderRepository; 
        private readonly IProductVariantRepository _productVariantRepository; // Thêm biến thể sản phẩm
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(IReviewRepository reviewRepository, IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<ReviewService> logger, IProductVariantRepository productVariantRepository)
        {
            _reviewRepository = reviewRepository;
            _orderRepository = orderRepository;
            _productVariantRepository = productVariantRepository; 
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PagedResponse<ReviewDto>> GetReviewsAsync(ReviewFilterDto filter)
        {
            // 1. Lấy dữ liệu phân trang từ Repository
            // `pagedReviews` bây giờ là một đối tượng PagedList<Review> hoàn chỉnh.
            var pagedReviews = await _reviewRepository.GetReviewsAsync(filter);

            // 2. Map từ danh sách Review sang danh sách ReviewDto
            // `pagedReviews.Items` chứa danh sách các đánh giá trên trang hiện tại.
            var reviewDtos = pagedReviews.Items.Select(MapToDto).ToList();

            // 3. TẠO ĐỐI TƯỢNG PagedResponse<ReviewDto> - BÂY GIỜ SẼ KHÔNG CÒN LỖI
            var response = new PagedResponse<ReviewDto>
            {
                Data = reviewDtos,
                // Ánh xạ 1-1 vì tên thuộc tính đã khớp nhau
                CurrentPage = pagedReviews.CurrentPage,
                PageSize = pagedReviews.PageSize,
                TotalCount = pagedReviews.TotalCount,
                TotalPages = pagedReviews.TotalPages,
                HasNextPage = pagedReviews.HasNextPage,
                HasPreviousPage = pagedReviews.HasPreviousPage,
                // Các thuộc tính khác
                Message = "Lấy danh sách đánh giá thành công.",
                IsSuccess = true
            };

            return response;
        }
        public async Task<ApiResult<IEnumerable<ReviewDto>>> GetReviewsForProductAsync(Guid productVariantId)
        {
            try
            {
                // 1. Tìm biến thể sản phẩm để lấy ProductId
                var variant = await _productVariantRepository.GetByIdAsync(productVariantId);
                if (variant == null)
                {
                    // Sử dụng phương thức Failure của ApiResult
                    return ApiResult<IEnumerable<ReviewDto>>.Failure("Không tìm thấy biến thể sản phẩm.");
                }

                // 2. Lấy tất cả review đã duyệt cho sản phẩm cha
                var reviews = await _reviewRepository.GetApprovedReviewsByProductIdAsync(variant.ProductId);

                // 3. Map kết quả sang DTO (Giả sử bạn có hàm MapToDto)
                var reviewDtos = reviews.Select(MapToDto).ToList();

                // Sử dụng phương thức Success của ApiResult
                return ApiResult<IEnumerable<ReviewDto>>.Success(
                    reviewDtos,
                    $"Lấy thành công {reviewDtos.Count} đánh giá cho sản phẩm."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đánh giá cho sản phẩm từ variantId {VariantId}", productVariantId);

                // Sử dụng phương thức Failure với thông tin Exception
                return ApiResult<IEnumerable<ReviewDto>>.Failure("Đã xảy ra lỗi hệ thống. Vui lòng thử lại.", ex);
            }
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

        public async Task<ApiResult<ReviewDto>> UpdateReviewAsync(Guid reviewId, UpdateReviewDto updateDto, Guid userId)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(reviewId);

                // 1. Kiểm tra sự tồn tại của review
                if (review == null)
                {
                    return ApiResult<ReviewDto>.Failure("Không tìm thấy bài đánh giá để cập nhật.");
                }

                // 2. Kiểm tra quyền sở hữu
                if (review.UserId != userId)
                {
                    return ApiResult<ReviewDto>.Failure("Bạn không có quyền sửa bài đánh giá này.");
                }

                // 3. Cập nhật các thuộc tính
                review.Rating = updateDto.Rating;
                review.Content = updateDto.Content;
                review.Images = updateDto.Images != null && updateDto.Images.Any()
                    ? string.Join(",", updateDto.Images)
                    : null;

                // Logic nghiệp vụ: Khi người dùng sửa, chuyển về trạng thái chờ duyệt lại
                review.Status = ReviewStatus.Pending;
                review.UpdatedAt = DateTime.UtcNow;

                await _reviewRepository.UpdateAsync(review);
                await _unitOfWork.SaveChangesAsync();

                // Lấy lại thông tin chi tiết để trả về
                var updatedReviewDetails = await _reviewRepository.GetReviewDetailsAsync(review.Id);
                return ApiResult<ReviewDto>.Success(MapToDto(updatedReviewDetails!), "Cập nhật đánh giá thành công. Đang chờ duyệt lại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật đánh giá {ReviewId} bởi người dùng {UserId}", reviewId, userId);
                return ApiResult<ReviewDto>.Failure("Đã có lỗi hệ thống xảy ra.");
            }
        }

        public async Task<ApiResult> DeleteReviewAsync(Guid reviewId, Guid userId)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(reviewId);

                // 1. Kiểm tra sự tồn tại
                if (review == null)
                {
                    return ApiResult.Failure("Không tìm thấy bài đánh giá để xóa.");
                }

                // 2. Kiểm tra quyền sở hữu (Hoặc có thể thêm logic cho Admin ở đây)
                if (review.UserId != userId)
                {
                    return ApiResult.Failure("Bạn không có quyền xóa bài đánh giá này.");
                }

                // 3. Thực hiện xóa mềm
                // GenericRepository của bạn đã có sẵn SoftDeleteAsync rất tốt!
                await _reviewRepository.SoftDeleteAsync(reviewId, userId);
                await _unitOfWork.SaveChangesAsync();

                return ApiResult.Success("Xóa đánh giá thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa đánh giá {ReviewId} bởi người dùng {UserId}", reviewId, userId);
                return ApiResult.Failure("Đã có lỗi hệ thống xảy ra.");
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
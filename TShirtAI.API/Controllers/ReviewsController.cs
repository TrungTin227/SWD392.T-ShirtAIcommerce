using DTOs.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

[Route("api/reviews")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ICurrentUserService _currentUserService;

    public ReviewsController(IReviewService reviewService, ICurrentUserService currentUserService)
    {
        _reviewService = reviewService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách các bài đánh giá (có phân trang và bộ lọc).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReviews([FromQuery] ReviewFilterDto filter)
    {
        var result = await _reviewService.GetReviewsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin thống kê review cho một biến thể sản phẩm.
    /// </summary>
    [HttpGet("stats/{productVariantId}")]
    public async Task<IActionResult> GetReviewStatistics(Guid productVariantId)
    {
        var stats = await _reviewService.GetReviewStatsAsync(productVariantId);
        if (stats == null)
        {
            return NotFound("Không tìm thấy thông tin thống kê cho sản phẩm này.");
        }
        return Ok(stats);
    }


    /// <summary>
    /// Lấy chi tiết một bài đánh giá.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReview(Guid id)
    {
        var review = await _reviewService.GetReviewByIdAsync(id);
        if (review == null)
        {
            return NotFound();
        }
        return Ok(review);
    }

    /// <summary>
    /// Tạo một bài đánh giá mới (yêu cầu đăng nhập).
    /// </summary>
    [HttpPost]
    [Authorize] // Chỉ người dùng đã đăng nhập mới được tạo review
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto createDto)
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("Không xác định được người dùng.");
        }

        try
        {
            var newReview = await _reviewService.CreateReviewAsync(createDto, userId.Value);
            if (newReview == null)
            {
                return BadRequest("Không thể tạo bài đánh giá. Vui lòng thử lại.");
            }
            return CreatedAtAction(nameof(GetReview), new { id = newReview.Id }, newReview);
        }
        catch (InvalidOperationException ex)
        {
            // Trả về lỗi nghiệp vụ rõ ràng cho client
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("product-by-variant/{variantId}")]
    [AllowAnonymous] // Thường thì ai cũng có thể xem review
    public async Task<IActionResult> GetProductReviews(Guid variantId)
    {
        var response = await _reviewService.GetReviewsForProductAsync(variantId);
        if (!response.IsSuccess)
        {
            return NotFound(response);
        }
        return Ok(response);
    }

    /// <summary>
    /// Cập nhật một bài đánh giá (yêu cầu đăng nhập và là chủ sở hữu).
    /// </summary>
    /// <param name="id">ID của bài đánh giá.</param>
    /// <param name="updateDto">Dữ liệu cần cập nhật.</param>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewDto updateDto)
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("Không xác định được người dùng.");
        }

        var result = await _reviewService.UpdateReviewAsync(id, updateDto, userId.Value);

        // Ensure all branches return IActionResult
        return result.Match<IActionResult>(
            onSuccess: data => Ok(data),
            onFailure: (message, ex) =>
            {
                if (message != null && message.Contains("không có quyền")) return Forbid();
                if (message != null && message.Contains("Không tìm thấy")) return NotFound(new { message });
                return BadRequest(new { message });
            }
        );
    }

    /// <summary>
    /// Xóa một bài đánh giá (yêu cầu đăng nhập và là chủ sở hữu).
    /// </summary>
    /// <param name="id">ID của bài đánh giá.</param>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("Không xác định được người dùng.");
        }

        var result = await _reviewService.DeleteReviewAsync(id, userId.Value);

        if (!result.IsSuccess)
        {
            if (result.Message.Contains("không có quyền")) return Forbid();
            if (result.Message.Contains("Không tìm thấy")) return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }

        // Trả về 204 No Content là chuẩn RESTful cho việc xóa thành công
        return NoContent();
    }
    [HttpGet("by-variant/{productVariantId}/my-review")]
    [Authorize] // Bắt buộc phải đăng nhập để biết "tôi" là ai
    public async Task<IActionResult> GetMyReviewForVariant(Guid productVariantId)
    {
        // Lấy ID của người dùng đang đăng nhập từ service
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            // Trường hợp này hiếm khi xảy ra nếu [Authorize] hoạt động đúng,
            // nhưng vẫn nên kiểm tra để đảm bảo an toàn.
            return Unauthorized("Không xác thực được người dùng.");
        }

        // Gọi service để lấy dữ liệu
        var reviewDto = await _reviewService.GetMyReviewForVariantAsync(productVariantId, userId.Value);

        // Nếu service trả về null, tức là không tìm thấy
        if (reviewDto == null)
        {
            // Trả về 404 Not Found là chuẩn RESTful trong trường hợp này
            return NotFound(new { message = "Bạn chưa đánh giá sản phẩm này." });
        }

        // Nếu tìm thấy, trả về dữ liệu với mã 200 OK
        return Ok(reviewDto);
    }

}
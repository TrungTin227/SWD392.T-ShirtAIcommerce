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
}
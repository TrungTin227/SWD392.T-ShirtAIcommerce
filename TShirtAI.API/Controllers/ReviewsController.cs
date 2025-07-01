using DTOs.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReviews([FromQuery] ReviewFilterDto filter)
        {
            var result = await _reviewService.GetReviewsAsync(filter);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetReviewById(Guid id)
        {
            var result = await _reviewService.GetByIdAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("product/{productId:guid}")]
        public async Task<IActionResult> GetProductReviews(Guid productId)
        {
            var result = await _reviewService.GetProductReviewsAsync(productId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("product/{productId:guid}/stats")]
        public async Task<IActionResult> GetProductReviewStats(Guid productId)
        {
            var result = await _reviewService.GetProductReviewStatsAsync(productId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("product/{productId:guid}/summary")]
        public async Task<IActionResult> GetProductReviewSummary(Guid productId)
        {
            var result = await _reviewService.GetProductReviewSummaryAsync(productId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("user/{userId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetUserReviews(Guid userId)
        {
            var result = await _reviewService.GetUserReviewsAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetPendingReviews()
        {
            var result = await _reviewService.GetPendingReviewsAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reviewService.CreateReviewAsync(createDto);
            return result.IsSuccess ? CreatedAtAction(nameof(GetReviewById), new { id = result.Data?.Id }, result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reviewService.UpdateReviewAsync(id, updateDto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}/admin")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> AdminUpdateReview(Guid id, [FromBody] AdminUpdateReviewDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reviewService.AdminUpdateReviewAsync(id, updateDto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var result = await _reviewService.DeleteReviewAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/helpful")]
        public async Task<IActionResult> MarkReviewHelpful(Guid id, [FromBody] bool isHelpful)
        {
            var result = await _reviewService.MarkReviewHelpfulAsync(id, isHelpful);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("can-review/{userId:guid}/{productId:guid}")]
        [Authorize]
        public async Task<IActionResult> CanUserReviewProduct(Guid userId, Guid productId)
        {
            var result = await _reviewService.CanUserReviewProductAsync(userId, productId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}

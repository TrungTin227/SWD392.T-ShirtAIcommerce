using DTOs.Wishlists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetWishlists([FromQuery] WishlistFilterDto filter)
        {
            var result = await _wishlistService.GetWishlistsAsync(filter);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("user/{userId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetUserWishlist(Guid userId)
        {
            var result = await _wishlistService.GetUserWishlistAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto addDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _wishlistService.AddToWishlistAsync(addDto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("user/{userId:guid}/product/{productId:guid}")]
        [Authorize]
        public async Task<IActionResult> RemoveFromWishlist(Guid userId, Guid productId)
        {
            var result = await _wishlistService.RemoveFromWishlistAsync(userId, productId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("user/{userId:guid}/product/{productId:guid}/check")]
        [Authorize]
        public async Task<IActionResult> IsProductInWishlist(Guid userId, Guid productId)
        {
            var result = await _wishlistService.IsProductInWishlistAsync(userId, productId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("user/{userId:guid}/clear")]
        [Authorize]
        public async Task<IActionResult> ClearUserWishlist(Guid userId)
        {
            var result = await _wishlistService.ClearUserWishlistAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetWishlistStats()
        {
            var result = await _wishlistService.GetWishlistStatsAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("stats/user/{userId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetUserWishlistSummary(Guid userId)
        {
            var result = await _wishlistService.GetUserWishlistSummaryAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopWishlistedProducts([FromQuery] int count = 10)
        {
            var result = await _wishlistService.GetTopWishlistedProductsAsync(count);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("product/{productId:guid}/count")]
        public async Task<IActionResult> GetProductWishlistCount(Guid productId)
        {
            var result = await _wishlistService.GetProductWishlistCountAsync(productId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("user/{userId:guid}/move-to-cart")]
        [Authorize]
        public async Task<IActionResult> MoveWishlistToCart(Guid userId, [FromBody] List<Guid> productIds)
        {
            if (productIds == null || !productIds.Any())
                return BadRequest("Product IDs are required");

            var result = await _wishlistService.MoveWishlistToCartAsync(userId, productIds);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
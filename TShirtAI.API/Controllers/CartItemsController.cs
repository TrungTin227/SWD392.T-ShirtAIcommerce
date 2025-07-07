using DTOs.Cart;
using DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartItemService _cartItemService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartItemService cartItemService,
            ICurrentUserService currentUserService,
            ILogger<CartController> logger)
        {
            _cartItemService = cartItemService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy giỏ hàng của người dùng hiện tại
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartItemDto>>> GetMyCart()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                var result = userId.HasValue
                    ? await _cartItemService.GetUserCartItemsAsync(userId.Value)
                    : await _cartItemService.GetSessionCartItemsAsync(sessionId);

                return result.IsSuccess
                    ? Ok(result.Data)
                    : BadRequest(new ErrorResponse { Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user cart");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy tổng quan giỏ hàng
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<CartSummaryDto>> GetCartSummary()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                var result = await _cartItemService.GetCartSummaryAsync(userId, sessionId);

                return result.IsSuccess
                    ? Ok(result.Data)
                    : BadRequest(new ErrorResponse { Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy tổng quan giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng (hỗ trợ 1 hoặc nhiều sản phẩm)
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<List<CartItemDto>>> AddToCart([FromBody] List<CreateCartItemDto> items)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                // Convert to internal DTOs
                var internalItems = new List<InternalCreateCartItemDto>();

                foreach (var item in items)
                {
                    decimal unitPrice = 0;
                    if (item.ProductVariantId.HasValue)
                    {
                        unitPrice = await _cartItemService.GetUnitPriceFromProductVariant(item.ProductVariantId.Value);
                    }
                    else
                    {
                        return BadRequest(new ErrorResponse { Message = "Không xác định được sản phẩm để lấy giá" });
                    }

                    internalItems.Add(new InternalCreateCartItemDto
                    {
                        CustomDesignId = item.CustomDesignId,
                        ProductVariantId = item.ProductVariantId,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        UserId = userId,
                        SessionId = userId.HasValue ? null : sessionId
                    });
                }

                var result = await _cartItemService.BulkAddToCartAsync(internalItems, userId, sessionId);

                return result.IsSuccess
                    ? Ok(result.Data)
                    : BadRequest(new ErrorResponse { Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi thêm vào giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật sản phẩm trong giỏ hàng
        /// </summary>
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<CartItemDto>> UpdateCartItem(Guid id, [FromBody] UpdateCartItemDto updateDto)
        {
            try
            {
                // Check ownership
                var cartItemResult = await _cartItemService.GetByIdAsync(id);
                if (!cartItemResult.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = cartItemResult.Message });

                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");

                if (!isAdmin)
                {
                    var cartItem = cartItemResult.Data;
                    if (cartItem.UserId != userId && cartItem.SessionId != sessionId)
                        return Forbid();
                }

                var result = await _cartItemService.UpdateCartItemAsync(id, updateDto);

                return result.IsSuccess
                    ? Ok(result.Data)
                    : BadRequest(new ErrorResponse { Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item {CartItemId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa sản phẩm khỏi giỏ hàng (hỗ trợ 1 hoặc nhiều sản phẩm)
        /// </summary>
        [HttpDelete]
        public async Task<ActionResult> RemoveFromCart([FromBody] List<Guid> cartItemIds)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                var result = await _cartItemService.BulkRemoveFromCartAsync(cartItemIds, userId, sessionId);

                return result.IsSuccess
                    ? NoContent()
                    : BadRequest(new ErrorResponse { Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cart");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi xóa khỏi giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        [HttpDelete("clear")]
        public async Task<ActionResult> ClearCart()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                var result = await _cartItemService.ClearCartAsync(userId, sessionId);

                return result.IsSuccess
                    ? NoContent()
                    : BadRequest(new ErrorResponse { Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi xóa giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Merge giỏ hàng guest vào tài khoản người dùng khi đăng nhập
        /// </summary>
        [HttpPost("merge")]
        [Authorize]
        public async Task<ActionResult> MergeGuestCart([FromQuery] string sessionId)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                if (!userId.HasValue)
                    return Unauthorized(new ErrorResponse { Message = "Người dùng chưa đăng nhập" });

                if (string.IsNullOrEmpty(sessionId))
                    return BadRequest(new ErrorResponse { Message = "Session ID là bắt buộc" });

                var result = await _cartItemService.MergeGuestCartToUserAsync(sessionId, userId.Value);

                return result.IsSuccess
                    ? Ok(new { Message = "Merge giỏ hàng thành công" })
                    : BadRequest(new ErrorResponse { Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging guest cart for session {SessionId}", sessionId);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi merge giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Kiểm tra tính khả dụng của cart trước khi checkout
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<CartValidationDto>> ValidateCart()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                var result = await _cartItemService.ValidateCartForCheckoutAsync(userId, sessionId);

                return result.IsSuccess
                    ? Ok(result.Data)
                    : BadRequest(new ErrorResponse { Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cart");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi kiểm tra giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật giá cho tất cả items trong giỏ hàng
        /// </summary>
        [HttpPost("update-prices")]
        public async Task<ActionResult> UpdateCartPrices()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                var result = await _cartItemService.UpdateCartPricesAsync(userId, sessionId);

                return result.IsSuccess
                    ? Ok(new { Message = result.Message })
                    : BadRequest(new ErrorResponse { Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart prices");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật giá giỏ hàng",
                    Details = ex.Message
                });
            }
        }
    }
}
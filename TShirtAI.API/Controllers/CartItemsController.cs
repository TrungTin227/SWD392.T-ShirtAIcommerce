using DTOs.Cart;
using DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Commons;
using Repositories.Helpers;
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
        [HttpGet("my-cart")]
        public async Task<ActionResult<IEnumerable<CartItemDto>>> GetMyCart()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                ApiResult<IEnumerable<CartItemDto>> result;

                if (userId.HasValue)
                {
                    result = await _cartItemService.GetUserCartItemsAsync(userId.Value);
                }
                else
                {
                    result = await _cartItemService.GetSessionCartItemsAsync(sessionId);
                }

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return Ok(result.Data);
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

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });


                return Ok(result.Data);
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
        /// Thêm sản phẩm vào giỏ hàng
        /// </summary>

        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<CartItemDto>> AddToCart([FromBody] CreateCartItemDto createDto)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                // Truy vấn giá từ Product hoặc ProductVariant:
                decimal unitPrice = 0;
                if (createDto.ProductVariantId.HasValue)
                {
                    unitPrice = await _cartItemService.GetUnitPriceFromProductVariant(createDto.ProductVariantId.Value);
                }
                else if (createDto.ProductId.HasValue)
                {
                    unitPrice = await _cartItemService.GetUnitPriceFromProduct(createDto.ProductId.Value);
                }
                else
                {
                    return BadRequest(new ErrorResponse { Message = "Không xác định được sản phẩm để lấy giá" });
                }

                var internalCreateDto = new InternalCreateCartItemDto
                {
                    UserId = userId,
                    SessionId = userId.HasValue ? null : sessionId,
                    ProductId = createDto.ProductId,
                    CustomDesignId = createDto.CustomDesignId,
                    ProductVariantId = createDto.ProductVariantId,
                    SelectedColor = createDto.SelectedColor,
                    SelectedSize = createDto.SelectedSize,
                    Quantity = createDto.Quantity,
                    UnitPrice = unitPrice
                };

                var result = await _cartItemService.AddToCartAsync(internalCreateDto);

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                // ✅ Thay đổi: Trả về Created với data trực tiếp thay vì CreatedAtAction
                return Created($"/api/cart/{result.Data.Id}", result.Data);

                // Hoặc đơn giản hơn, chỉ trả về Ok:
                // return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart {@CreateDto}", createDto);
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
                // Check ownership first
                var cartItemResult = await _cartItemService.GetByIdAsync(id);
                if (!cartItemResult.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = cartItemResult.Message });

                var userId = _currentUserService.GetUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
                var sessionId = HttpContext.Session.Id;

                if (!isAdmin)
                {
                    var cartItem = cartItemResult.Data;
                    if (cartItem.UserId != userId && cartItem.SessionId != sessionId)
                        return Forbid();
                }

                var result = await _cartItemService.UpdateCartItemAsync(id, updateDto);

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return Ok(result.Data);
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
        /// Xóa sản phẩm khỏi giỏ hàng
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveFromCart(Guid id)
        {
            try
            {
                // Check ownership first
                var cartItemResult = await _cartItemService.GetByIdAsync(id);
                if (!cartItemResult.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = cartItemResult.Message });

                var userId = _currentUserService.GetUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
                var sessionId = HttpContext.Session.Id;

                if (!isAdmin)
                {
                    var cartItem = cartItemResult.Data;
                    if (cartItem.UserId != userId && cartItem.SessionId != sessionId)
                        return Forbid();
                }

                var result = await _cartItemService.RemoveFromCartAsync(id);

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cart {CartItemId}", id);
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

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return NoContent();
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

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return Ok(new { Message = "Merge giỏ hàng thành công" });
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

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return Ok(result.Data);
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
    }
}
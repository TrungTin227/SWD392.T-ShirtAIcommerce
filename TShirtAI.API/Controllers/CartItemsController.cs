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
        /// Lấy danh sách sản phẩm trong giỏ hàng (Admin/Staff)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<PagedList<CartItemDto>>> GetCartItems([FromQuery] CartItemQueryDto query)
        {
            try
            {
                var result = await _cartItemService.GetCartItemsAsync(query);

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart items with query {@Query}", query);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy danh sách giỏ hàng",
                    Details = ex.Message
                });
            }
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
        /// Lấy số lượng sản phẩm trong giỏ hàng
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCartItemCount()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                var result = await _cartItemService.GetCartItemCountAsync(userId, sessionId);

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi đếm sản phẩm trong giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy tổng tiền giỏ hàng
        /// </summary>
        [HttpGet("total")]
        public async Task<ActionResult<decimal>> GetCartTotal()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                var result = await _cartItemService.GetCartTotalAsync(userId, sessionId);

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart total");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi tính tổng giỏ hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết sản phẩm trong giỏ hàng
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CartItemDto>> GetCartItem(Guid id)
        {
            try
            {
                var result = await _cartItemService.GetByIdAsync(id);

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                // Check ownership for non-admin users
                var userId = _currentUserService.GetUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
                var sessionId = HttpContext.Session.Id;

                if (!isAdmin)
                {
                    var cartItem = result.Data;
                    if (cartItem.UserId != userId && cartItem.SessionId != sessionId)
                        return Forbid();
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item {CartItemId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy thông tin sản phẩm trong giỏ hàng",
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
                // 🔒 Tự động lấy UserId từ current user
                var userId = _currentUserService.GetUserId();
                var sessionId = HttpContext.Session.Id;

                // 🔒 Tạo internal DTO với UserId/SessionId được set tự động
                var internalCreateDto = new InternalCreateCartItemDto
                {
                    UserId = userId,
                    SessionId = userId.HasValue ? null : sessionId, // Nếu có user thì không dùng session
                    ProductId = createDto.ProductId,
                    CustomDesignId = createDto.CustomDesignId,
                    ProductVariantId = createDto.ProductVariantId,
                    SelectedColor = createDto.SelectedColor,
                    SelectedSize = createDto.SelectedSize,
                    Quantity = createDto.Quantity,
                    UnitPrice = createDto.UnitPrice
                };

                var result = await _cartItemService.AddToCartAsync(internalCreateDto);

                if (!result.IsSuccess)
                    return BadRequest(new ErrorResponse { Message = result.Message });

                return CreatedAtAction(nameof(GetCartItem), new { id = result.Data.Id }, result.Data);
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
    }
}
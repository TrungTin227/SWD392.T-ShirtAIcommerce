using DTOs.Common;
using DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            ICurrentUserService currentUserService,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng với filtering và pagination (Admin/Staff)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<PagedResponse<OrderDTO>>> GetOrders([FromQuery] OrderFilterRequest filter)
        {
            try
            {
                var result = await _orderService.GetOrdersAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders with filter {@Filter}", filter);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy danh sách đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách đơn hàng của người dùng hiện tại
        /// </summary>
        [HttpGet("my-orders")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetMyOrders()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                if (!userId.HasValue)
                    return Unauthorized(new ErrorResponse { Message = "Người dùng chưa đăng nhập" });

                var orders = await _orderService.GetUserOrdersAsync(userId.Value);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy danh sách đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết đơn hàng
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDTO>> GetOrder(Guid id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                    return NotFound(new ErrorResponse { Message = "Không tìm thấy đơn hàng" });

                // Check if user owns the order or is admin/staff
                var userId = _currentUserService.GetUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");

                if (!isAdmin && (!userId.HasValue || order.UserId != userId.Value))
                    return Forbid();

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy thông tin đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Tạo đơn hàng mới (user)
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<OrderDTO>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                if (!userId.HasValue)
                    return Unauthorized(new ErrorResponse { Message = "Người dùng chưa đăng nhập" });

                var order = await _orderService.CreateOrderAsync(request, userId);

                if (order == null)
                    return BadRequest(new ErrorResponse { Message = "Không thể tạo đơn hàng" });

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order {@Request}", request);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi tạo đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng (user)
        /// </summary>
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<OrderDTO>> UpdateOrder(Guid id, [FromBody] UpdateOrderRequest request)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var order = await _orderService.UpdateOrderAsync(id, request, userId);

                if (order == null)
                    return NotFound(new ErrorResponse { Message = "Không tìm thấy đơn hàng để cập nhật" });

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa đơn hàng (soft delete, chỉ Admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteOrder(Guid id)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var result = await _orderService.DeleteOrderAsync(id, userId);

                if (!result)
                    return NotFound(new ErrorResponse { Message = "Không tìm thấy đơn hàng để xóa" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi xóa đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng (Chỉ Admin/Staff)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Staff")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var result = await _orderService.UpdateOrderStatusAsync(id, request.Status, userId);

                if (!result)
                    return BadRequest(new ErrorResponse { Message = "Không thể cập nhật trạng thái đơn hàng" });

                return Ok(new { Message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật trạng thái",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái thanh toán (Chỉ Admin/Staff)
        /// </summary>
        [HttpPatch("{id}/payment-status")]
        [Authorize(Roles = "Admin,Staff")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult> UpdatePaymentStatus(Guid id, [FromBody] UpdatePaymentStatusRequest request)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var result = await _orderService.UpdatePaymentStatusAsync(id, request.PaymentStatus, userId);

                if (!result)
                    return BadRequest(new ErrorResponse { Message = "Không thể cập nhật trạng thái thanh toán" });

                return Ok(new { Message = "Cập nhật trạng thái thanh toán thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật trạng thái thanh toán",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Phân công đơn hàng cho nhân viên (Chỉ Admin)
        /// </summary>
        [HttpPatch("{id}/assign-staff")]
        [Authorize(Roles = "Admin")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult> AssignStaff(Guid id, [FromBody] AssignStaffRequest request)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var result = await _orderService.AssignOrderToStaffAsync(id, request.StaffId, userId);

                if (!result)
                    return BadRequest(new ErrorResponse { Message = "Không thể phân công đơn hàng" });

                return Ok(new { Message = "Phân công nhân viên thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning staff to order {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi phân công nhân viên",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Hủy đơn hàng (User hoặc Admin/Staff)
        /// </summary>
        [HttpPatch("{id}/cancel")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                // Check if user owns the order or is admin/staff
                var userId = _currentUserService.GetUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");

                if (!isAdmin)
                {
                    var isOwner = await _orderService.IsOrderOwnedByUserAsync(id, userId ?? Guid.Empty);
                    if (!isOwner)
                        return Forbid();
                }

                var result = await _orderService.CancelOrderAsync(id, request.Reason, userId);

                if (!result)
                    return BadRequest(new ErrorResponse { Message = "Không thể hủy đơn hàng" });

                return Ok(new { Message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi hủy đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách đơn hàng được phân công cho nhân viên
        /// </summary>
        [HttpGet("staff/{staffId}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetStaffOrders(Guid staffId)
        {
            try
            {
                // Staff can only see their own orders, Admin can see all
                var currentUserId = _currentUserService.GetUserId();
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin && currentUserId != staffId)
                    return Forbid();

                var orders = await _orderService.GetStaffOrdersAsync(staffId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff orders for {StaffId}", staffId);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy danh sách đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật mã vận đơn (Admin/Staff)
        /// </summary>
        [HttpPatch("{id}/tracking")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult> UpdateTrackingNumber(Guid id, [FromBody] UpdateTrackingRequest request)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var result = await _orderService.UpdateTrackingNumberAsync(id, request.TrackingNumber, userId);

                if (!result)
                    return BadRequest(new ErrorResponse { Message = "Không thể cập nhật mã vận đơn" });

                return Ok(new { Message = "Cập nhật mã vận đơn thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracking number for order {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật mã vận đơn",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái hàng loạt (Chỉ Admin)
        /// </summary>
        [HttpPatch("bulk-update-status")]
        [Authorize(Roles = "Admin")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<BatchOperationResultDTO>> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                var result = await _orderService.BulkUpdateStatusAsync(request.OrderIds, request.Status, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk update status operation");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật hàng loạt",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thống kê trạng thái đơn hàng (Admin/Staff)
        /// </summary>
        [HttpGet("statistics/status-counts")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<Dictionary<string, int>>> GetOrderStatusCounts()
        {
            try
            {
                var counts = await _orderService.GetOrderStatusCountsAsync();
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status counts");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy thống kê",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách đơn hàng gần đây (Admin/Staff)
        /// </summary>
        [HttpGet("recent")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetRecentOrders([FromQuery] int limit = 10)
        {
            try
            {
                var orders = await _orderService.GetRecentOrdersAsync(limit);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy danh sách đơn hàng gần đây",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy dữ liệu đơn hàng cho báo cáo (Admin)
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrdersForAnalytics(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                var orders = await _orderService.GetOrdersForAnalyticsAsync(fromDate, toDate);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for analytics");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy dữ liệu phân tích",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Tính tổng tiền đơn hàng
        /// </summary>
        [HttpGet("{id}/total")]
        public async Task<ActionResult<decimal>> CalculateOrderTotal(Guid id)
        {
            try
            {
                // Check if user owns the order or is admin/staff
                var userId = _currentUserService.GetUserId();
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");

                if (!isAdmin)
                {
                    var isOwner = await _orderService.IsOrderOwnedByUserAsync(id, userId ?? Guid.Empty);
                    if (!isOwner)
                        return Forbid();
                }

                var total = await _orderService.CalculateOrderTotalAsync(id);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order total {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi tính tổng tiền",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Tạo mã đơn hàng mới (Admin/Staff)
        /// </summary>
        [HttpGet("generate-order-number")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<string>> GenerateOrderNumber()
        {
            try
            {
                var orderNumber = await _orderService.GenerateOrderNumberAsync();
                return Ok(orderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating order number");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi tạo mã đơn hàng",
                    Details = ex.Message
                });
            }
        }
    }

    // Additional DTO for tracking update
    public class UpdateTrackingRequest
    {
        [Required(ErrorMessage = "Mã vận đơn là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Mã vận đơn không được vượt quá 100 ký tự")]
        public string TrackingNumber { get; set; } = string.Empty;
    }
}
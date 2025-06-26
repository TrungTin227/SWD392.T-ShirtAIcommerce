using DTOs.Common;
using DTOs.Orders;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng với filtering và pagination
        /// </summary>
        [HttpGet]
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
        /// Tạo đơn hàng mới
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<OrderDTO>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId(); // Helper method to get current user
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
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<OrderDTO>> UpdateOrder(Guid id, [FromBody] UpdateOrderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var order = await _orderService.UpdateOrderAsync(id, request, userId);

                if (order == null)
                    return NotFound(new ErrorResponse { Message = "Không tìm thấy đơn hàng để cập nhật" });

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} with {@Request}", id, request);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa đơn hàng (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
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
        /// Lấy danh sách đơn hàng của user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetUserOrders(Guid userId)
        {
            try
            {
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders for {UserId}", userId);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy danh sách đơn hàng của user",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.UpdateOrderStatusAsync(id, request.Status, userId);

                if (!result)
                    return BadRequest(new ErrorResponse { Message = "Không thể cập nhật trạng thái đơn hàng" });

                return Ok(new { Message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status {OrderId} to {Status}", id, request.Status);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái thanh toán
        /// </summary>
        [HttpPatch("{id}/payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus(Guid id, [FromBody] UpdatePaymentStatusRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.UpdatePaymentStatusAsync(id, request.PaymentStatus, userId);

                if (!result)
                    return BadRequest(new ErrorResponse { Message = "Không thể cập nhật trạng thái thanh toán" });

                return Ok(new { Message = "Cập nhật trạng thái thanh toán thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status {OrderId} to {Status}", id, request.PaymentStatus);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật trạng thái thanh toán",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Gán đơn hàng cho nhân viên
        /// </summary>
        [HttpPatch("{id}/assign")]
        public async Task<IActionResult> AssignOrder(Guid id, [FromBody] AssignOrderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.AssignOrderToStaffAsync(id, request.StaffId, userId);

                if (!result)
                    return BadRequest(new ErrorResponse { Message = "Không thể gán đơn hàng cho nhân viên" });

                return Ok(new { Message = "Gán đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning order {OrderId} to staff {StaffId}", id, request.StaffId);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi gán đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.CancelOrderAsync(id, request.Reason, userId);

                if (!result)
                    return BadRequest(new ErrorResponse { Message = "Không thể hủy đơn hàng" });

                return Ok(new { Message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling order {OrderId}", id);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi hủy đơn hàng",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy đơn hàng được gán cho nhân viên
        /// </summary>
        [HttpGet("staff/{staffId}")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetStaffOrders(Guid staffId)
        {
            try
            {
                var orders = await _orderService.GetStaffOrdersAsync(staffId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff orders for {StaffId}", staffId);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi lấy danh sách đơn hàng của nhân viên",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật hàng loạt trạng thái đơn hàng
        /// </summary>
        [HttpPatch("bulk/status")]
        public async Task<ActionResult<BatchOperationResultDTO>> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.BulkUpdateStatusAsync(request.OrderIds, request.Status, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating order status {@Request}", request);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Có lỗi xảy ra khi cập nhật hàng loạt trạng thái đơn hàng",
                    Details = ex.Message
                });
            }
        }

    }

    // Request DTOs for specific endpoints
    public class UpdateStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class UpdatePaymentStatusRequest
    {
        [Required]
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class AssignOrderRequest
    {
        [Required]
        public Guid StaffId { get; set; }
    }

    public class CancelOrderRequest
    {
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class BulkUpdateStatusRequest
    {
        [Required]
        [MinLength(1)]
        public List<Guid> OrderIds { get; set; } = new();

        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
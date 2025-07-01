using BusinessObjects.Products;
using DTOs.Common;
using DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
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
            => await ExecuteAsync(() => _orderService.GetOrdersAsync(filter), "Error getting orders with filter {@Filter}", filter);

        /// <summary>
        /// Lấy danh sách đơn hàng của người dùng hiện tại
        /// </summary>
        [HttpGet("my-orders")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetMyOrders()
        {
            var userId = GetCurrentUserIdOrUnauthorized();
            if (userId == null) return Unauthorized(ErrorResponse("Người dùng chưa đăng nhập"));

            return await ExecuteAsync(() => _orderService.GetUserOrdersAsync(userId.Value), "Error getting user orders");
        }

        /// <summary>
        /// Lấy thông tin chi tiết đơn hàng
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDTO>> GetOrder(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null) return NotFound(ErrorResponse("Không tìm thấy đơn hàng"));

                if (!CanAccessOrder(order.UserId)) return Forbid();

                return Ok(order);
            }, "Error getting order {OrderId}", id);
        }

        /// <summary>
        /// Tạo đơn hàng mới (user)
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<OrderDTO>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userId = GetCurrentUserIdOrUnauthorized();
            if (userId == null) return Unauthorized(ErrorResponse("Người dùng chưa đăng nhập"));

            return await ExecuteAsync(async () =>
            {
                var order = await _orderService.CreateOrderAsync(request, userId);
                return order == null
                    ? BadRequest(ErrorResponse("Không thể tạo đơn hàng"))
                    : CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }, "Error creating order {@Request}", request);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng (user)
        /// </summary>
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult<OrderDTO>> UpdateOrder(Guid id, [FromBody] UpdateOrderRequest request)
        {
            var userId = GetCurrentUserIdOrUnauthorized();
            if (userId == null) return Unauthorized(ErrorResponse("Người dùng chưa đăng nhập"));

            return await ExecuteAsync(async () =>
            {
                if (!IsAdminOrStaff() && !await _orderService.IsOrderOwnedByUserAsync(id, userId.Value))
                    return Forbid();

                var order = await _orderService.UpdateOrderAsync(id, request, userId);
                return order == null
                    ? NotFound(ErrorResponse("Không tìm thấy đơn hàng để cập nhật"))
                    : Ok(order);
            }, "Error updating order {OrderId}", id);
        }

        /// <summary>
        /// Xóa đơn hàng (soft delete, chỉ Admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteOrder(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                var userId = _currentUserService.GetUserId();
                var result = await _orderService.DeleteOrderAsync(id, userId);
                return result ? NoContent() : NotFound(ErrorResponse("Không tìm thấy đơn hàng để xóa"));
            }, "Error deleting order {OrderId}", id);
        }

            /// <summary>
            /// Cập nhật trạng thái đơn hàng (Chỉ Admin/Staff)
            /// </summary>
            [HttpPatch("{id}/status")]
            [Authorize(Roles = "Admin,Staff")]
            public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = _currentUserService.GetUserId();
                var result = await _orderService.UpdateOrderStatusAsync(id, request.Status, userId);

                if (result)
                    return Ok(new { Message = "Cập nhật trạng thái thành công" });

                return BadRequest(new { Error = "Không thể cập nhật trạng thái đơn hàng" });
            }

            /// <summary>
            /// Cập nhật trạng thái thanh toán (Chỉ Admin/Staff)
            /// </summary>
            [HttpPatch("{id}/payment-status")]
            [Authorize(Roles = "Admin,Staff")]
            public async Task<IActionResult> UpdatePaymentStatus(Guid id, [FromBody] UpdatePaymentStatusRequest request)
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = _currentUserService.GetUserId();
                var result = await _orderService.UpdatePaymentStatusAsync(id, request.PaymentStatus, userId);

                if (result)
                    return Ok(new { Message = "Cập nhật trạng thái thanh toán thành công" });

                return BadRequest(new { Error = "Không thể cập nhật trạng thái thanh toán" });
            }           

        /// <summary>
        /// Hủy đơn hàng (User hoặc Admin/Staff)
        /// </summary>
        [HttpPatch("{id}/cancel")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<ActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest request)
        {
            var userId = GetCurrentUserIdOrUnauthorized();
            if (userId == null) return Unauthorized(ErrorResponse("Người dùng chưa đăng nhập"));

            return await ExecuteAsync(async () =>
            {
                if (!IsAdminOrStaff() && !await _orderService.IsOrderOwnedByUserAsync(id, userId.Value))
                    return Forbid();

                var result = await _orderService.CancelOrderAsync(id, request.Reason, userId);
                return result
                    ? Ok(new { Message = "Hủy đơn hàng thành công" })
                    : BadRequest(ErrorResponse("Không thể hủy đơn hàng"));
            }, "Error cancelling order {OrderId}", id);
        }
            
        /// <summary>
        /// Cập nhật trạng thái hàng loạt (Chỉ Admin)
        /// </summary>
        [HttpPatch("bulk-update-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var currentUserId = _currentUserService.GetUserId();
            var result = await _orderService
                .BulkUpdateStatusAsync(request.OrderIds, request.Status, currentUserId);
            return Ok(result);
        }


        /// <summary>
        /// Lấy thống kê trạng thái đơn hàng (Admin/Staff)
        /// </summary>
        [HttpGet("statistics/status-counts")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<Dictionary<OrderStatus, int>>> GetOrderStatusCounts()
        {
            var result = await _orderService.GetOrderStatusCountsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách đơn hàng gần đây (Admin/Staff)
        /// </summary>
        [HttpGet("recent")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetRecentOrders([FromQuery] int limit = 10)
            => await ExecuteAsync(() => _orderService.GetRecentOrdersAsync(limit), "Error getting recent orders");

        /// <summary>
        /// Lấy dữ liệu đơn hàng cho báo cáo (Admin)
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrdersForAnalytics(
            [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
            => await ExecuteAsync(() => _orderService.GetOrdersForAnalyticsAsync(fromDate, toDate),
                "Error getting orders for analytics");

        /// <summary>
        /// Tính tổng tiền đơn hàng
        /// </summary>
        [HttpGet("{id}/total")]
        public async Task<ActionResult<decimal>> CalculateOrderTotal(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                var userId = _currentUserService.GetUserId();
                if (!IsAdminOrStaff() && !await _orderService.IsOrderOwnedByUserAsync(id, userId ?? Guid.Empty))
                    return Forbid();

                var total = await _orderService.CalculateOrderTotalAsync(id);
                return Ok(total);
            }, "Error calculating order total {OrderId}", id);
        }
       
        #region Helper Methods

        private Guid? GetCurrentUserIdOrUnauthorized()
            => _currentUserService.GetUserId();

        private bool IsAdminOrStaff()
            => User.IsInRole("Admin") || User.IsInRole("Staff");

        private bool CanAccessOrder(Guid orderUserId)
        {
            if (IsAdminOrStaff()) return true;
            var userId = _currentUserService.GetUserId();
            return userId.HasValue && orderUserId == userId.Value;
        }

        private static ErrorResponse ErrorResponse(string message)
            => new() { Message = message };

        private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> operation, string errorMessage, params object[] args)
        {
            try
            {
                var result = await operation();
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, errorMessage, args);
                return BadRequest(ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, errorMessage, args);
                return BadRequest(ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, errorMessage, args);
                return StatusCode(500, ErrorResponse("Có lỗi xảy ra khi xử lý yêu cầu"));
            }
        }

        private async Task<ActionResult> ExecuteAsync(Func<Task<ActionResult>> operation, string errorMessage, params object[] args)
        {
            try
            {
                return await operation();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, errorMessage, args);
                return BadRequest(ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, errorMessage, args);
                return BadRequest(ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, errorMessage, args);
                return StatusCode(500, ErrorResponse("Có lỗi xảy ra khi xử lý yêu cầu"));
            }
        }       
        #endregion
    }
}
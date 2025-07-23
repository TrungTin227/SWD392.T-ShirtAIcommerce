using BusinessObjects.Common;
using DTOs.Common;
using DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Helpers;
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
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
        {
            try
            {
                var result = await _orderService.CreateOrderAsync(req, _currentUserService.GetUserId());

                return Ok(new
                {
                    success = true,
                    order = result.Order,
                    payment = result.Payment,
                    paymentUrl = result.PaymentUrl,    
                    message = "Đơn hàng đã được tạo thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateOrder failed for {@Request}", req);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        /// <summary>
        /// Cập nhật trạng thái đơn hàng (Chỉ Admin/Staff)
        /// </summary>
            [HttpPatch("{id}/status")]
            //[Authorize(Roles = "Admin,Staff")]
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
        /// Hủy đơn hàng (User hoặc Admin/Staff)
        /// </summary>
        [HttpPatch("{id}/cancel")]
        //[ServiceFilter(typeof(ValidateModelAttribute))]
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
        /// Cập nhật đơn hàng (User có thể cập nhật một số thông tin)
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
        /// Cập nhật trạng thái thanh toán (Admin/Staff)
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
        /// Lấy thống kê đơn hàng theo trạng thái (Admin/Staff)
        /// </summary>
        [HttpGet("statistics/status-counts")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<Dictionary<OrderStatus, int>>> GetOrderStatusCounts()
        {
            return await ExecuteAsync(() => _orderService.GetOrderStatusCountsAsync(),
                "Error getting order status counts");
        }

        /// <summary>
        /// Lấy đơn hàng gần đây (Admin/Staff)
        /// </summary>
        [HttpGet("recent")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetRecentOrders([FromQuery] int limit = 10)
        {
            return await ExecuteAsync(() => _orderService.GetRecentOrdersAsync(limit),
                "Error getting recent orders");
        }

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


        [HttpPut("batch/process")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> BulkProcessOrders([FromBody] List<Guid> orderIds)
        {
            if (orderIds == null || !orderIds.Any())
                return BadRequest(new { success = false, message = "Danh sách đơn hàng không được để trống." });

            var staffId = _currentUserService.GetUserId();
            if (!staffId.HasValue)
                return Unauthorized(new { success = false, message = "Bạn chưa đăng nhập." });

            var result = await _orderService.BulkProcessOrdersAsync(orderIds, staffId.Value);
            return Ok(result);
        }


        [HttpPost("{orderId}/confirm")]
        public async Task<IActionResult> ConfirmOrder(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null || order.Status != OrderStatus.Paid)
                return BadRequest("Đơn chưa ở trạng thái Paid");

            await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Completed);
            return Ok(new { Message = "Đơn đã được staff xác nhận (Completed)" });
        }

        [HttpPut("batch/mark-shipping")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> BulkMarkOrdersAsShipping([FromBody] List<Guid> orderIds)
        {
            if (orderIds == null || !orderIds.Any())
                return BadRequest("Danh sách đơn hàng không được để trống");

            var staffId = _currentUserService.GetUserId();
            if (!staffId.HasValue)
                return Unauthorized();

            var result = await _orderService.BulkMarkOrdersAsShippingAsync(orderIds, staffId.Value);
            return Ok(result);
        }

        [HttpPut("batch/confirm-delivered")]
        [Authorize(Roles = "Customer,Staff,Admin")]
        public async Task<IActionResult> BulkConfirmDelivered([FromBody] List<Guid> orderIds)
        {
            if (orderIds == null || !orderIds.Any())
                return BadRequest("Danh sách đơn hàng không được để trống");

            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _orderService.BulkConfirmDeliveredByUserAsync(orderIds, userId.Value);
            return Ok(result);
        }


        [HttpPut("batch/complete")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> BulkCompleteCODOrders([FromBody] List<Guid> orderIds)
        {
            if (orderIds == null || !orderIds.Any())
                return BadRequest("Danh sách đơn hàng không được để trống");

            var staffId = _currentUserService.GetUserId();
            if (!staffId.HasValue)
                return Unauthorized();

            var result = await _orderService.BulkCompleteCODOrdersAsync(orderIds, staffId.Value);
            return Ok(result);
        }

        [HttpDelete("bulk-delete")]
        public async Task<IActionResult> BulkDeleteOrders([FromBody] List<Guid> orderIds)
        {
            if (orderIds == null || !orderIds.Any())
            {
                return BadRequest("Danh sách ID đơn hàng không được để trống.");
            }

            var userId = Guid.Parse(User.FindFirst("uid")!.Value);

            var result = await _orderService.BulkDeleteOrdersAsync(orderIds, userId);

            if (result.FailureCount > 0 && result.SuccessCount == 0)
            {
                // Nếu tất cả đều thất bại do validation
                return BadRequest(result);
            }

            // Trả về 200 OK với kết quả chi tiết, ngay cả khi có một số thất bại
            return Ok(result);
        }
        [HttpPost("purge-completed")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PurgeCompletedOrders([FromBody] PurgeOrdersRequest request)
        {


            // Bây giờ bạn có thể sử dụng biến adminId một cách an toàn
            var result = await _orderService.PurgeCompletedOrdersAsync(request.DaysOld);

            if ((result.IsPartialSuccess || result.IsCompleteFailure) && result.TotalRequested > 0)
            {
                return StatusCode(500, result);
            }

            return Ok(result);
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

        #region Enhanced Order Management Endpoints

        /// <summary>
        /// Tạo đơn hàng từ giỏ hàng
        /// </summary>
        [HttpPost("from-cart")]
        public async Task<ActionResult<OrderDTO>> CreateOrderFromCart([FromBody] CreateOrderFromCartRequest request)
        {
            try
            {
                var userId = GetCurrentUserIdOrUnauthorized();
                if (userId == null) return Unauthorized(ErrorResponse("Người dùng chưa đăng nhập"));

                var result = await _orderService.CreateOrderFromCartAsync(request, userId.Value);

                if (result != null)
                {
                    return Ok(result);
                }

                return BadRequest(ErrorResponse("Không thể tạo đơn hàng từ giỏ hàng"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order from cart for user {UserId}", _currentUserService.GetUserId());
                return StatusCode(500, ErrorResponse("Có lỗi xảy ra khi tạo đơn hàng"));
            }
        }

        /// <summary>
        /// Validate giỏ hàng trước khi tạo đơn hàng
        /// </summary>
        [HttpPost("validate-cart")]
        public async Task<ActionResult<OrderValidationResult>> ValidateCartForOrder([FromQuery] string? sessionId = null)
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                if (!userId.HasValue && string.IsNullOrEmpty(sessionId))
                {
                    sessionId = HttpContext.Session.Id;
                }

                var result = await _orderService.ValidateCartForOrderAsync(userId, sessionId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cart for order");
                return StatusCode(500, ErrorResponse("Có lỗi xảy ra khi kiểm tra giỏ hàng"));
            }
        }

        /// <summary>
        /// Tính toán lại tổng tiền đơn hàng
        /// </summary>
        [HttpPost("{orderId}/recalculate-total")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<decimal>> RecalculateOrderTotal(Guid orderId)
        {
            try
            {
                var total = await _orderService.RecalculateOrderTotalAsync(orderId);
                return Ok(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating order total for {OrderId}", orderId);
                return StatusCode(500, ErrorResponse("Có lỗi xảy ra khi tính toán lại tổng tiền"));
            }
        }

        /// <summary>
        /// Lấy analytics đơn hàng nâng cao
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<OrderAnalyticsDto>> GetOrderAnalytics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var analytics = await _orderService.GetOrderAnalyticsAsync(from, to);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order analytics");
                return StatusCode(500, ErrorResponse("Có lỗi xảy ra khi lấy thống kê đơn hàng"));
            }
        }

        /// <summary>
        /// Bulk hủy nhiều đơn hàng
        /// </summary>
        [HttpPost("bulk-cancel")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<BatchOperationResultDTO>> BulkCancelOrders([FromBody] BulkCancelOrdersRequest request)
        {
            try
            {
                var cancelledBy = _currentUserService.GetUserId();
                var result = await _orderService.BulkCancelOrdersAsync(request.OrderIds, request.Reason, cancelledBy);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk cancelling orders");
                return StatusCode(500, ErrorResponse("Có lỗi xảy ra khi hủy đơn hàng hàng loạt"));
            }
        }

        /// <summary>
        /// Reserve inventory cho đơn hàng
        /// </summary>
        [HttpPost("{orderId}/reserve-inventory")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult> ReserveInventoryForOrder(Guid orderId)
        {
            try
            {
                var success = await _orderService.ReserveInventoryForOrderAsync(orderId);
                
                if (success)
                {
                    return Ok(new { Message = "Reserve inventory thành công" });
                }
                
                return BadRequest(ErrorResponse("Không thể reserve inventory"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving inventory for order {OrderId}", orderId);
                return StatusCode(500, ErrorResponse("Có lỗi xảy ra khi reserve inventory"));
            }
        }

        /// <summary>
        /// Release inventory cho đơn hàng bị hủy
        /// </summary>
        [HttpPost("{orderId}/release-inventory")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult> ReleaseInventoryForOrder(Guid orderId)
        {
            try
            {
                var success = await _orderService.ReleaseInventoryForOrderAsync(orderId);
                
                if (success)
                {
                    return Ok(new { Message = "Release inventory thành công" });
                }
                
                return BadRequest(ErrorResponse("Không thể release inventory"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing inventory for order {OrderId}", orderId);
                return StatusCode(500, ErrorResponse("Có lỗi xảy ra khi release inventory"));
            }
        }

        #endregion

        #endregion
        /// <summary>
        /// Yêu cầu hủy đơn hàng (User hoặc Admin/Staff).
        /// Đơn ở trạng thái 'Pending' hoặc 'Processing' sẽ được hủy trực tiếp.
        /// Đơn ở trạng thái 'Delivered' hoặc 'Completed' sẽ chuyển sang trạng thái 'CancellationRequested'
        /// và cần Admin/Staff duyệt.
        /// </summary>
        [HttpPatch("{id}/request-cancellation")]
        [ServiceFilter(typeof(ValidateModelAttribute))] // Đảm bảo DTO RequestCancellationRequest được validate
        public async Task<ActionResult> RequestCancellation(Guid id, [FromBody] RequestCancellationRequest request)
        {
            var userId = GetCurrentUserIdOrUnauthorized();
            if (userId == null) return Unauthorized(ErrorResponse("Người dùng chưa đăng nhập."));

            return await ExecuteAsync(async () =>
            {
                // Logic kiểm tra quyền truy cập của người dùng được xử lý trong OrderService.RequestOrderCancellationAsync
                // thông qua _currentUserService.IsAdmin() || _currentUserService.IsStaff()
                var result = await _orderService.RequestOrderCancellationAsync(id, request, userId);
                return result
                    ? Ok(new { Message = "Yêu cầu hủy đơn hàng đã được gửi thành công. Đơn hàng sẽ được hủy trực tiếp (nếu đủ điều kiện) hoặc đang chờ duyệt." })
                    : BadRequest(ErrorResponse("Không thể gửi yêu cầu hủy đơn hàng."));
            }, "Lỗi khi gửi yêu cầu hủy đơn hàng {OrderId}", id);
        }

        /// <summary>
        /// Xử lý yêu cầu hủy đơn hàng (Duyệt/Từ chối) - Chỉ Admin/Staff.
        /// Áp dụng cho các đơn hàng có trạng thái 'CancellationRequested'.
        /// </summary>
        [HttpPatch("{id}/process-cancellation-request")]
        [Authorize(Roles = "Admin,Staff")]
        [ServiceFilter(typeof(ValidateModelAttribute))] // Đảm bảo DTO ProcessCancellationRequest được validate
        public async Task<ActionResult> ProcessCancellationRequest(Guid id, [FromBody] ProcessCancellationRequest request)
        {
            var staffId = _currentUserService.GetUserId();
            if (!staffId.HasValue) return Unauthorized(ErrorResponse("Bạn chưa đăng nhập hoặc không có quyền."));

            return await ExecuteAsync(async () =>
            {
                var result = await _orderService.ProcessCancellationRequestAsync(id, request, staffId.Value);
                return result
                    ? Ok(new { Message = "Yêu cầu hủy đơn hàng đã được xử lý thành công." })
                    : BadRequest(ErrorResponse("Không thể xử lý yêu cầu hủy đơn hàng."));
            }, "Lỗi khi xử lý yêu cầu hủy đơn hàng {OrderId}", id);
        }
        [HttpGet("cancelled")]
        [Authorize] // Yêu cầu người dùng phải đăng nhập
        public async Task<ActionResult<PagedList<CancelledOrderDto>>> GetCancelledOrders([FromQuery] PaginationParams paginationParams)
        {
            return await ExecuteAsync(
                () => _orderService.GetCancelledOrdersAsync(paginationParams),
                "Lỗi khi lấy danh sách đơn hàng đã hủy."
            );
        }
    }
}
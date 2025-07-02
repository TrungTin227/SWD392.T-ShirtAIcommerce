using BusinessObjects.Products;
using DTOs.Common;
using DTOs.Orders;

namespace Services.Interfaces
{
    public interface IOrderService
    {
        Task<PagedResponse<OrderDTO>> GetOrdersAsync(OrderFilterRequest filter);
        Task<OrderDTO?> GetOrderByIdAsync(Guid orderId);
        Task<OrderDTO?> CreateOrderAsync(CreateOrderRequest request, Guid? createdBy = null);
        Task<OrderDTO?> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, Guid? updatedBy = null);
        Task<bool> DeleteOrderAsync(Guid orderId, Guid? deletedBy = null);
        Task<IEnumerable<OrderDTO>> GetUserOrdersAsync(Guid userId);
        Task<bool> IsOrderOwnedByUserAsync(Guid orderId, Guid userId);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, Guid? updatedBy = null);
        Task<bool> UpdatePaymentStatusAsync(Guid orderId, PaymentStatus paymentStatus, Guid? updatedBy = null);
        Task<bool> AssignOrderToStaffAsync(Guid orderId, Guid staffId, Guid? updatedBy = null);
        Task<bool> CancelOrderAsync(Guid orderId, string reason, Guid? cancelledBy = null);
        Task<IEnumerable<OrderDTO>> GetStaffOrdersAsync(Guid staffId);
        Task<BatchOperationResultDTO> BulkUpdateStatusAsync(List<Guid> orderIds, OrderStatus status, Guid? updatedBy = null);
        Task<decimal> CalculateOrderTotalAsync(Guid orderId);
        Task<string> GenerateOrderNumberAsync();
        Task<Dictionary<OrderStatus, int>> GetOrderStatusCountsAsync();
        Task<IEnumerable<OrderDTO>> GetRecentOrdersAsync(int limit = 10);
        Task<IEnumerable<OrderDTO>> GetOrdersForAnalyticsAsync(DateTime fromDate, DateTime toDate);
        Task<bool> UpdateTrackingNumberAsync(Guid orderId, string trackingNumber, Guid? updatedBy = null);

        /// <summary>
        /// Tạo đơn hàng từ giỏ hàng với validation đầy đủ
        /// </summary>
        Task<OrderDTO?> CreateOrderFromCartAsync(CreateOrderFromCartRequest request, Guid userId);

        /// <summary>
        /// Validate giỏ hàng trước khi tạo đơn hàng
        /// </summary>
        Task<OrderValidationResult> ValidateCartForOrderAsync(Guid? userId, string? sessionId);

        /// <summary>
        /// Reserve inventory cho đơn hàng
        /// </summary>
        Task<bool> ReserveInventoryForOrderAsync(Guid orderId);

        /// <summary>
        /// Release inventory khi hủy đơn hàng
        /// </summary>
        Task<bool> ReleaseInventoryForOrderAsync(Guid orderId);

        /// <summary>
        /// Tính toán lại tổng tiền đơn hàng
        /// </summary>
        Task<decimal> RecalculateOrderTotalAsync(Guid orderId);

        /// <summary>
        /// Lấy order analytics nâng cao
        /// </summary>
        Task<OrderAnalyticsDto> GetOrderAnalyticsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Bulk cancel orders
        /// </summary>
        Task<BatchOperationResultDTO> BulkCancelOrdersAsync(List<Guid> orderIds, string reason, Guid? cancelledBy = null);
    }
}
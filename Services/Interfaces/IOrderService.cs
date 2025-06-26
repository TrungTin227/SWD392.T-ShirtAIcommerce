using DTOs.Common;
using DTOs.Orders;

namespace Services.Interfaces
{
    public interface IOrderService
    {
        // CRUD Operations
        Task<PagedResponse<OrderDTO>> GetOrdersAsync(OrderFilterRequest filter);
        Task<OrderDTO?> GetOrderByIdAsync(Guid orderId);
        Task<OrderDTO?> CreateOrderAsync(CreateOrderRequest request, Guid? createdBy = null);
        Task<OrderDTO?> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, Guid? updatedBy = null);
        Task<bool> DeleteOrderAsync(Guid orderId, Guid? deletedBy = null);

        // User specific operations
        Task<IEnumerable<OrderDTO>> GetUserOrdersAsync(Guid userId);
        Task<bool> IsOrderOwnedByUserAsync(Guid orderId, Guid userId);

        // Status management
        Task<bool> UpdateOrderStatusAsync(Guid orderId, string status, Guid? updatedBy = null);
        Task<bool> UpdatePaymentStatusAsync(Guid orderId, string paymentStatus, Guid? updatedBy = null);
        Task<bool> CancelOrderAsync(Guid orderId, string reason, Guid? cancelledBy = null);

        // Staff operations
        Task<bool> AssignOrderToStaffAsync(Guid orderId, Guid staffId, Guid? updatedBy = null);
        Task<IEnumerable<OrderDTO>> GetStaffOrdersAsync(Guid staffId);

        // Bulk operations
        Task<BatchOperationResultDTO> BulkUpdateStatusAsync(List<Guid> orderIds, string status, Guid? updatedBy = null);

        // Business logic
        Task<decimal> CalculateOrderTotalAsync(Guid orderId);
        Task<string> GenerateOrderNumberAsync();

        // Analytics and reporting
        Task<Dictionary<string, int>> GetOrderStatusCountsAsync();
        Task<IEnumerable<OrderDTO>> GetRecentOrdersAsync(int limit = 10);
        Task<IEnumerable<OrderDTO>> GetOrdersForAnalyticsAsync(DateTime fromDate, DateTime toDate);

        // Tracking
        Task<bool> UpdateTrackingNumberAsync(Guid orderId, string trackingNumber, Guid? updatedBy = null);
    }
}
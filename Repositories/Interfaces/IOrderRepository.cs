using BusinessObjects.Orders;
using BusinessObjects.Products;
using DTOs.Orders;
using Repositories.Helpers;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order, Guid>
    {
        Task<PagedList<Order>> GetOrdersAsync(OrderFilterRequest filter);
        Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId);
        Task<Order?> GetOrderWithDetailsAsync(Guid orderId);
        Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status);
        Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, Guid? updatedBy = null);
        Task<bool> UpdatePaymentStatusAsync(Guid orderId, PaymentStatus paymentStatus, Guid? updatedBy = null);
        Task<bool> AssignOrderToStaffAsync(Guid orderId, Guid staffId, Guid? updatedBy = null);
        Task<string> GenerateOrderNumberAsync();
        Task<decimal> CalculateOrderTotalAsync(Guid orderId);
        Task<bool> IsOrderOwnedByUserAsync(Guid orderId, Guid userId);
        Task<IEnumerable<Order>> GetStaffOrdersAsync(Guid staffId);
        Task<bool> CancelOrderAsync(Guid orderId, string reason, Guid? cancelledBy = null);
        Task<IEnumerable<Order>> GetOrdersForAnalyticsAsync(DateTime fromDate, DateTime toDate);
        Task<Dictionary<OrderStatus, int>> GetOrderStatusCountsAsync();
        Task<IEnumerable<Order>> GetRecentOrdersAsync(int limit = 10);
        Task<bool> UpdateTrackingNumberAsync(Guid orderId, string trackingNumber, Guid? updatedBy = null);
    }
}
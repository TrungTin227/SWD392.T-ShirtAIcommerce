using BusinessObjects.Common;
using BusinessObjects.Orders;
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


        /// <summary>
        /// Tính tổng doanh thu từ các đơn hàng có trạng thái là "Completed".
        /// </summary>
        Task<decimal> GetTotalRevenueFromCompletedOrdersAsync();

        /// <summary>
        /// Đếm số lượng đơn hàng trong một khoảng thời gian.
        /// </summary>
        Task<int> GetOrderCountAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Lấy số lượng đơn hàng cho mỗi trạng thái thanh toán.
        /// </summary>
        Task<Dictionary<PaymentStatus, int>> GetPaymentStatusCountsAsync();

        /// <summary>
        /// Lấy danh sách các đơn hàng đã bị hủy có phân trang.
        /// </summary>
        /// <param name="paginationParams">Thông số phân trang.</param>
        /// <param name="userId">ID của người dùng (nếu có) để lọc theo người dùng.</param>
        /// <returns>Danh sách các đơn hàng đã hủy được phân trang.</returns>
        Task<PagedList<Order>> GetCancelledOrdersAsync(PaginationParams paginationParams, Guid? userId = null);

    }
}
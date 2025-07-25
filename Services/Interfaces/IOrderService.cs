﻿using BusinessObjects.Common;
using DTOs.Analytics;
using DTOs.Common;
using DTOs.Orders;
using Repositories.Helpers;
using System;

namespace Services.Interfaces
{
    public interface IOrderService
    {
        Task<PagedResponse<OrderDTO>> GetOrdersAsync(OrderFilterRequest filter);
        Task<OrderDTO?> GetOrderByIdAsync(Guid orderId);
        Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, Guid? userId);
        Task<OrderDTO?> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, Guid? updatedBy = null);
        Task<BatchOperationResultDTO> BulkDeleteOrdersAsync(List<Guid> orderIds, Guid? deletedBy = null);
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

        Task<BatchOperationResultDTO> BulkMarkOrdersAsShippingAsync(List<Guid> orderIds, Guid staffId);
        Task<BatchOperationResultDTO> BulkConfirmDeliveredByUserAsync(List<Guid> orderIds, Guid userId);
        Task<BatchOperationResultDTO> BulkCompleteCODOrdersAsync(List<Guid> orderIds, Guid staffId);
        Task<BatchOperationResultDTO> BulkProcessOrdersAsync(List<Guid> orderIds, Guid staffId);
        Task<DashboardAnalyticsDto?> GetDashboardAnalyticsAsync();

        Task<BatchOperationResultDTO> PurgeCompletedOrdersAsync(int daysOld);

        Task<bool> RequestOrderCancellationAsync(Guid orderId, RequestCancellationRequest request, Guid? userId);

        /// <summary>
        /// Xử lý yêu cầu hủy đơn hàng (Admin/Staff). Duyệt hoặc từ chối yêu cầu.
        /// </summary>
        /// <param name="orderId">ID đơn hàng.</param>
        /// <param name="request">Trạng thái xử lý (Duyệt/Từ chối) và ghi chú của Admin.</param>
        /// <param name="staffId">ID của Admin/Staff thực hiện xử lý.</param>
        /// <returns>True nếu yêu cầu được xử lý thành công, ngược lại là False.</returns>
        Task<bool> ProcessCancellationRequestAsync(Guid orderId, ProcessCancellationRequest request, Guid staffId);
        /// <summary>
        /// Lấy danh sách các đơn hàng đã hủy có phân trang.
        /// </summary>
        /// <param name="paginationParams">Thông số phân trang.</param>
        /// <returns>Một đối tượng PagedList chứa các Order DTOs.</returns>
        Task<PagedList<CancelledOrderDto>> GetCancelledOrdersAsync(PaginationParams paginationParams);
    }
}
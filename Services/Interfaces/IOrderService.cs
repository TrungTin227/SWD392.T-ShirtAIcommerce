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
        Task<bool> UpdateOrderStatusAsync(Guid orderId, string status, Guid? updatedBy = null);
        Task<bool> UpdatePaymentStatusAsync(Guid orderId, string paymentStatus, Guid? updatedBy = null);
        Task<bool> AssignOrderToStaffAsync(Guid orderId, Guid staffId, Guid? updatedBy = null);
        Task<bool> CancelOrderAsync(Guid orderId, string reason, Guid? cancelledBy = null);
        Task<IEnumerable<OrderDTO>> GetStaffOrdersAsync(Guid staffId);
        Task<BatchOperationResultDTO> BulkUpdateStatusAsync(List<Guid> orderIds, string status, Guid? updatedBy = null);
    }
}
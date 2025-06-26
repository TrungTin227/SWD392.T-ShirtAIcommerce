using DTOs.OrderItem;
using Repositories.Commons;
using Repositories.Helpers;

namespace Services.Interfaces
{
    public interface IOrderItemService
    {
        Task<ApiResult<OrderItemDto>> CreateAsync(CreateOrderItemDto dto);
        Task<ApiResult<OrderItemDto>> UpdateAsync(UpdateOrderItemDto dto);
        Task<ApiResult<bool>> DeleteAsync(Guid id);
        Task<ApiResult<OrderItemDto?>> GetByIdAsync(Guid id);
        Task<ApiResult<PagedList<OrderItemDto>>> GetOrderItemsAsync(OrderItemQueryDto query);
        Task<ApiResult<IEnumerable<OrderItemDto>>> GetByOrderIdAsync(Guid orderId);
        Task<ApiResult<decimal>> GetOrderTotalAsync(Guid orderId);
        Task<ApiResult<int>> GetOrderItemCountAsync(Guid orderId);
    }
}
using BusinessObjects.Orders;
using DTOs.OrderItem;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IOrderItemRepository : IGenericRepository<OrderItem, Guid>
    {
        Task<PagedList<OrderItem>> GetOrderItemsAsync(OrderItemQueryDto query);
        Task<IEnumerable<OrderItem>> GetByOrderIdAsync(Guid orderId);
        Task<OrderItem?> GetWithDetailsAsync(Guid id);
        Task<bool> ValidateOrderExistsAsync(Guid orderId);
        Task<bool> ValidateProductExistsAsync(Guid productId);
        Task<bool> ValidateCustomDesignExistsAsync(Guid customDesignId);
        Task<bool> ValidateProductVariantExistsAsync(Guid productVariantId);
        Task<decimal> GetOrderTotalAsync(Guid orderId);
        Task<int> GetOrderItemCountAsync(Guid orderId);
    }
}
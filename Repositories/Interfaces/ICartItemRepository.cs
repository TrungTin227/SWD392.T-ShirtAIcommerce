using BusinessObjects.Cart;
using DTOs.Cart;
using Repositories.Helpers;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface ICartItemRepository : IGenericRepository<CartItem, Guid>
    {
        Task<PagedList<CartItem>> GetCartItemsAsync(CartItemQueryDto query);
        Task<IEnumerable<CartItem>> GetUserCartItemsAsync(Guid userId);
        Task<IEnumerable<CartItem>> GetSessionCartItemsAsync(string sessionId);
        Task<CartItem?> GetWithDetailsAsync(Guid id);
        Task<CartItem?> FindExistingCartItemAsync(Guid? userId, string? sessionId, Guid? productId, Guid? customDesignId, Guid? productVariantId, string? selectedColor, string? selectedSize);
        Task<bool> ClearUserCartAsync(Guid userId);
        Task<bool> ClearSessionCartAsync(string sessionId);
        Task<decimal> GetCartTotalAsync(Guid? userId, string? sessionId);
        Task<int> GetCartItemCountAsync(Guid? userId, string? sessionId);
        Task<bool> MergeGuestCartToUserAsync(string sessionId, Guid userId);
        Task<bool> ValidateProductExistsAsync(Guid productId);
        Task<bool> ValidateCustomDesignExistsAsync(Guid customDesignId);
        Task<bool> ValidateProductVariantExistsAsync(Guid productVariantId);
    }
}
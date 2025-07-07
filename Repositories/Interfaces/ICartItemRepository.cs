using BusinessObjects.Cart;
using BusinessObjects.CustomDesigns;
using BusinessObjects.Products;
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
        Task<CartItem?> FindExistingCartItemAsync(Guid? userId, string? sessionId, Guid? customDesignId, Guid? productVariantId);
        Task<bool> ClearUserCartAsync(Guid userId);
        Task<bool> ClearSessionCartAsync(string sessionId);
        Task<decimal> GetCartTotalAsync(Guid? userId, string? sessionId);
        Task<int> GetCartItemCountAsync(Guid? userId, string? sessionId);
        Task<bool> MergeGuestCartToUserAsync(string sessionId, Guid userId);
        Task<bool> ValidateProductExistsAsync(Guid productId);
        Task<bool> ValidateCustomDesignExistsAsync(Guid customDesignId);
        Task<bool> ValidateProductVariantExistsAsync(Guid productVariantId);
        Task<Product?> GetProductByIdAsync(Guid productId);
        Task<ProductVariant?> GetProductVariantByIdAsync(Guid productVariantId);
        /// <summary>
        /// Lấy cart items của user với đầy đủ navigation properties
        /// </summary>
        Task<IEnumerable<CartItem>> GetUserCartItemsWithDetailsAsync(Guid userId);

        /// <summary>
        /// Lấy cart items của session với đầy đủ navigation properties
        /// </summary>
        Task<IEnumerable<CartItem>> GetSessionCartItemsWithDetailsAsync(string sessionId);

        /// <summary>
        /// Lấy CustomDesign by ID
        /// </summary>
        Task<CustomDesign?> GetCustomDesignByIdAsync(Guid customDesignId);
    }
}
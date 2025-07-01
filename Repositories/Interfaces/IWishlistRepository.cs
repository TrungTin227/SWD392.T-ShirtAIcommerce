using BusinessObjects.Wishlists;
using DTOs.Wishlists;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IWishlistRepository : IGenericRepository<WishlistItem, Guid>
    {
        Task<IEnumerable<WishlistItem>> GetUserWishlistAsync(Guid userId);
        Task<WishlistItem?> GetWishlistItemAsync(Guid userId, Guid productId);
        Task<bool> IsProductInWishlistAsync(Guid userId, Guid productId);
        Task<bool> RemoveFromWishlistAsync(Guid userId, Guid productId);
        Task<IEnumerable<WishlistItem>> GetWishlistsByProductIdAsync(Guid productId);
        Task<WishlistStatsDto> GetWishlistStatsAsync();
        Task<UserWishlistSummaryDto> GetUserWishlistSummaryAsync(Guid userId);
        Task<IEnumerable<ProductWishlistStatsDto>> GetTopWishlistedProductsAsync(int count = 10);
        Task<int> GetProductWishlistCountAsync(Guid productId);
        Task<bool> ClearUserWishlistAsync(Guid userId);
    }
}
using DTOs.Wishlists;
using Repositories.Commons;
using Repositories.Helpers;

namespace Services.Interfaces
{
    public interface IWishlistService
    {
        Task<ApiResult<List<WishlistItemDto>>> GetUserWishlistAsync(Guid userId);
        Task<ApiResult<PagedList<WishlistItemDto>>> GetWishlistsAsync(WishlistFilterDto filter);
        Task<ApiResult<WishlistItemDto>> AddToWishlistAsync(AddToWishlistDto addDto);
        Task<ApiResult<bool>> RemoveFromWishlistAsync(Guid userId, Guid productId);
        Task<ApiResult<bool>> IsProductInWishlistAsync(Guid userId, Guid productId);
        Task<ApiResult<bool>> ClearUserWishlistAsync(Guid userId);
        Task<ApiResult<WishlistStatsDto>> GetWishlistStatsAsync();
        Task<ApiResult<UserWishlistSummaryDto>> GetUserWishlistSummaryAsync(Guid userId);
        Task<ApiResult<List<ProductWishlistStatsDto>>> GetTopWishlistedProductsAsync(int count = 10);
        Task<ApiResult<int>> GetProductWishlistCountAsync(Guid productId);
        Task<ApiResult<bool>> MoveWishlistToCartAsync(Guid userId, List<Guid> productIds);
    }
}
using DTOs.Cart;
using Repositories.Commons;
using Repositories.Helpers;

namespace Services.Interfaces
{
    public interface ICartItemService
    {
        Task<ApiResult<CartItemDto>> GetByIdAsync(Guid id);
        Task<ApiResult<PagedList<CartItemDto>>> GetCartItemsAsync(CartItemQueryDto query);
        Task<ApiResult<IEnumerable<CartItemDto>>> GetUserCartItemsAsync(Guid userId);
        Task<ApiResult<IEnumerable<CartItemDto>>> GetSessionCartItemsAsync(string sessionId);
        Task<ApiResult<CartSummaryDto>> GetCartSummaryAsync(Guid? userId, string? sessionId);
        Task<ApiResult<CartItemDto>> AddToCartAsync(InternalCreateCartItemDto createDto);
        Task<ApiResult<CartItemDto>> UpdateCartItemAsync(Guid id, UpdateCartItemDto updateDto);
        Task<ApiResult<bool>> RemoveFromCartAsync(Guid id);
        Task<ApiResult<bool>> ClearCartAsync(Guid? userId, string? sessionId);
        Task<ApiResult<bool>> MergeGuestCartToUserAsync(string sessionId, Guid userId);
        Task<ApiResult<int>> GetCartItemCountAsync(Guid? userId, string? sessionId);
        Task<ApiResult<decimal>> GetCartTotalAsync(Guid? userId, string? sessionId);
    }
}
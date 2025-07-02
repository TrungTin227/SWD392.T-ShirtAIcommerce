using BusinessObjects.Cart;
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
        Task<decimal> GetUnitPriceFromProduct(Guid productId);
        Task<decimal> GetUnitPriceFromProductVariant(Guid productVariantId);

        Task<ApiResult<IEnumerable<CartItemDto>>> GetCartItemsByIdsAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId);
        Task<ApiResult<bool>> ClearCartItemsByIdsAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId);
        Task<ApiResult<IEnumerable<CartItem>>> GetCartItemsForCheckoutAsync(Guid? userId, string? sessionId);
        /// <summary>
        /// Kiểm tra cart có thể checkout không
        /// </summary>
        Task<ApiResult<CartValidationDto>> ValidateCartForCheckoutAsync(Guid? userId, string? sessionId);
        /// <summary>
        /// Lấy CartItem entities với đầy đủ navigation properties cho checkout
        /// </summary>
        Task<ApiResult<IEnumerable<CartItem>>> GetCartItemEntitiesForCheckoutAsync(Guid? userId, string? sessionId);

        /// <summary>
        /// Xóa cart items sau khi checkout thành công
        /// </summary>
        Task<ApiResult<bool>> ClearCartItemsAfterCheckoutAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId);

        /// <summary>
        /// Validate giỏ hàng chi tiết cho checkout
        /// </summary>
        Task<ApiResult<CartValidationDto>> ValidateCartForCheckoutDetailedAsync(Guid? userId, string? sessionId);

    }
}
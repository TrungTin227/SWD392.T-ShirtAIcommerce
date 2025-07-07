using BusinessObjects.Cart;
using DTOs.Cart;
using Repositories.Commons;

namespace Services.Interfaces
{
    public interface ICartItemService
    {
        Task<ApiResult<CartItemDto>> GetByIdAsync(Guid id);
        Task<ApiResult<IEnumerable<CartItemDto>>> GetUserCartItemsAsync(Guid userId);
        Task<ApiResult<IEnumerable<CartItemDto>>> GetSessionCartItemsAsync(string sessionId);
        Task<ApiResult<CartSummaryDto>> GetCartSummaryAsync(Guid? userId, string? sessionId);
        Task<ApiResult<CartItemDto>> UpdateCartItemAsync(Guid id, UpdateCartItemDto updateDto);
        Task<ApiResult<bool>> ClearCartAsync(Guid? userId, string? sessionId);
        Task<ApiResult<bool>> MergeGuestCartToUserAsync(string sessionId, Guid userId);
        Task<decimal> GetUnitPriceFromProduct(Guid productId);
        Task<decimal> GetUnitPriceFromProductVariant(Guid productVariantId);

        Task<ApiResult<bool>> ClearCartItemsByIdsAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId);
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

        /// <summary>
        /// Cập nhật giá cho tất cả items trong giỏ hàng
        /// </summary>
        Task<ApiResult<bool>> UpdateCartPricesAsync(Guid? userId, string? sessionId);

        /// <summary>
        /// Bulk thêm/cập nhật nhiều items vào giỏ hàng
        /// </summary>
        Task<ApiResult<List<CartItemDto>>> BulkAddToCartAsync(List<InternalCreateCartItemDto> items, Guid? userId, string? sessionId);

        /// <summary>
        /// Bulk xóa nhiều items khỏi giỏ hàng
        /// </summary>
        Task<ApiResult<bool>> BulkRemoveFromCartAsync(List<Guid> cartItemIds, Guid? userId, string? sessionId);

        /// <summary>
        /// Lấy thống kê giỏ hàng
        /// </summary>
        Task<ApiResult<CartAnalyticsDto>> GetCartAnalyticsAsync(Guid? userId, string? sessionId);

    }
}
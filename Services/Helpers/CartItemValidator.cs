using BusinessObjects.Common;
using DTOs.Cart;
using Repositories.Commons;
using Repositories.Interfaces;

namespace Services.Helpers
{
    public static class CartItemValidator
    {
        public static async Task<ApiResult<CartItemDto>> ValidateAddAsync(InternalCreateCartItemDto dto, ICartItemRepository repo)
        {
            int idCount = 0;
            if (dto.ProductVariantId.HasValue) idCount++;
            if (dto.CustomDesignId.HasValue) idCount++;
            if (dto.ProductId.HasValue) idCount++;

            if (idCount == 0)
                return ApiResult<CartItemDto>.Failure("Phải có ít nhất một trong các ID: ProductVariantId hoặc CustomDesignId");
            if (idCount > 1)
                return ApiResult<CartItemDto>.Failure("Chỉ được chọn 1 trong các trường ProductVariantId, CustomDesignId, ProductId");

            if (dto.ProductVariantId.HasValue)
            {
                var variant = await repo.GetProductVariantByIdAsync(dto.ProductVariantId.Value);
                if (variant == null || !variant.IsActive)
                    return ApiResult<CartItemDto>.Failure("Biến thể sản phẩm không tồn tại hoặc đã ngưng bán");
                if (variant.Quantity < dto.Quantity)
                    return ApiResult<CartItemDto>.Failure($"Chỉ còn {variant.Quantity} sản phẩm trong kho");
            }
            else if (dto.CustomDesignId.HasValue)
            {
                var exists = await repo.ValidateCustomDesignExistsAsync(dto.CustomDesignId.Value);
                if (!exists)
                    return ApiResult<CartItemDto>.Failure("Thiết kế tuỳ chỉnh không tồn tại");
            }
            else if (dto.ProductId.HasValue)
            {
                var product = await repo.GetProductByIdAsync(dto.ProductId.Value);
                if (product == null || product.IsDeleted || product.Status != ProductStatus.Active)
                    return ApiResult<CartItemDto>.Failure("Sản phẩm không tồn tại hoặc đã ngưng bán");
            }

            if (dto.Quantity <= 0)
                return ApiResult<CartItemDto>.Failure("Số lượng không hợp lệ");
            if (dto.UnitPrice <= 0)
                return ApiResult<CartItemDto>.Failure("Đơn giá không hợp lệ");

            return ApiResult<CartItemDto>.Success(null);
        }

        public static ApiResult<CartItemDto> ValidateUpdate(UpdateCartItemDto updateDto)
        {
            if (updateDto.Quantity <= 0)
                return ApiResult<CartItemDto>.Failure("Số lượng không hợp lệ");
            return ApiResult<CartItemDto>.Success(null);
        }
    }
}
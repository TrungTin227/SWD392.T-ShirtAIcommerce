using BusinessObjects.Cart;
using DTOs.Cart;
using Repositories.Helpers;
using Services.Helpers;

namespace Services.Helpers.Mappers
{
    public static class CartItemMapper
    {
        public static CartItemDto ToDto(CartItem entity)
        {
            return new CartItemDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                SessionId = entity.SessionId,
                ProductId = entity.ProductId,
                CustomDesignId = entity.CustomDesignId,
                ProductVariantId = entity.ProductVariantId,
                Quantity = entity.Quantity,
                UnitPrice = entity.UnitPrice,
                TotalPrice = entity.TotalPrice,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UserName = entity.User?.UserName,
                ProductName = entity.Product?.Name,
                CustomDesignName = entity.CustomDesign?.DesignName,
                ProductVariantName = entity.ProductVariant == null
                    ? null
                    : $"{entity.ProductVariant.Color} - {entity.ProductVariant.Size}"
            };
        }

        public static CartItem ToEntity(InternalCreateCartItemDto dto)
        {
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                SessionId = dto.SessionId,
                ProductId = dto.ProductId,
                CustomDesignId = dto.CustomDesignId,
                ProductVariantId = dto.ProductVariantId,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return cartItem;
        }
        public static void UpdateEntity(CartItem entity, UpdateCartItemDto dto)
        {
            entity.Quantity = dto.Quantity;
            entity.UnitPrice = dto.UnitPrice;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        public static IEnumerable<CartItemDto> ToDtoList(IEnumerable<CartItem> entities)
        {
            return entities.Select(ToDto);
        }

        public static PagedList<CartItemDto> ToPagedDto(PagedList<CartItem> pagedEntities)
        {
            var dtoList = pagedEntities.Select(ToDto).ToList();
            return new PagedList<CartItemDto>(
                dtoList,
                pagedEntities.MetaData.TotalCount,
                pagedEntities.MetaData.CurrentPage,
                pagedEntities.MetaData.PageSize
            );
        }

        public static CartSummaryDto ToCartSummaryDto(IEnumerable<CartItem> cartItems, decimal estimatedShipping = 0, decimal estimatedTax = 0)
        {
            var items = cartItems.ToList();
            var subtotal = items.Sum(ci => ci.TotalPrice);

            return new CartSummaryDto
            {
                TotalItems = items.Count,
                TotalQuantity = items.Sum(ci => ci.Quantity),
                SubTotal = subtotal,
                EstimatedShipping = estimatedShipping,
                EstimatedTax = estimatedTax,
                EstimatedTotal = subtotal + estimatedShipping + estimatedTax,
                Items = ToDtoList(items).ToList()
            };
        }
    }
}
﻿using BusinessObjects.Cart;
using DTOs.Cart;
using Repositories.Helpers;

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

                // SỬA Ở ĐÂY: Lấy tên Product thông qua ProductVariant
                ProductName = entity.ProductVariant?.Product?.Name,

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
            entity.UpdatedAt = DateTime.UtcNow;
        }

        public static List<CartItemDto> ToDtoList(IEnumerable<CartItem> entities)
        {
            return entities.Select(ToDto).ToList();
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
                EstimatedTax = estimatedTax,
                EstimatedTotal = subtotal + estimatedTax,
                Items = ToDtoList(items).ToList()
            };
        }
    }
}
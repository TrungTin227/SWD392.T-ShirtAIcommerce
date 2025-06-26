using BusinessObjects.Orders;
using DTOs.OrderItem;
using Repositories.Helpers;
using Services.Helpers;

namespace Services.Helpers.Mappers
{
    public static class OrderItemMapper
    {
        public static OrderItemDto ToDto(OrderItem entity)
        {
            return new OrderItemDto
            {
                Id = entity.Id,
                OrderId = entity.OrderId,
                ProductId = entity.ProductId,
                CustomDesignId = entity.CustomDesignId,
                ProductVariantId = entity.ProductVariantId,
                ItemName = entity.ItemName,
                SelectedColor = entity.SelectedColor,
                SelectedSize = entity.SelectedSize,
                Quantity = entity.Quantity,
                UnitPrice = entity.UnitPrice,
                TotalPrice = entity.TotalPrice,
                ProductName = entity.Product?.Name,
                CustomDesignName = entity.CustomDesign?.Name,
                VariantName = entity.ProductVariant?.Name
            };
        }

        public static OrderItem ToEntity(CreateOrderItemDto dto, decimal unitPrice)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = dto.OrderId,
                ProductId = dto.ProductId,
                CustomDesignId = dto.CustomDesignId,
                ProductVariantId = dto.ProductVariantId,
                ItemName = dto.ItemName,
                SelectedColor = dto.SelectedColor,
                SelectedSize = dto.SelectedSize,
                Quantity = dto.Quantity,
                UnitPrice = unitPrice
            };

            // Calculate total price using business logic
            orderItem.TotalPrice = OrderItemBusinessLogic.CalculateTotalPrice(orderItem.UnitPrice, orderItem.Quantity);

            return orderItem;
        }

        public static void UpdateEntity(OrderItem entity, UpdateOrderItemDto dto)
        {
            entity.ItemName = dto.ItemName;
            entity.SelectedColor = dto.SelectedColor;
            entity.SelectedSize = dto.SelectedSize;
            entity.Quantity = dto.Quantity;

            // Recalculate total price when quantity changes
            entity.TotalPrice = OrderItemBusinessLogic.CalculateTotalPrice(entity.UnitPrice, entity.Quantity);
        }

        public static IEnumerable<OrderItemDto> ToDtoList(IEnumerable<OrderItem> entities)
        {
            return entities.Select(ToDto);
        }

        public static PagedList<OrderItemDto> ToPagedDto(PagedList<OrderItem> pagedEntities)
        {
            var dtoList = pagedEntities.Select(ToDto).ToList();
            return new PagedList<OrderItemDto>(
                dtoList,
                pagedEntities.MetaData.TotalCount,
                pagedEntities.MetaData.CurrentPage,
                pagedEntities.MetaData.PageSize
            );
        }
    }
}
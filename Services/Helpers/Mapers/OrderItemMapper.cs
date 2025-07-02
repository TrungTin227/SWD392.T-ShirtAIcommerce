using BusinessObjects.Cart;
using BusinessObjects.Orders;
using DTOs.OrderItem;
using Repositories.Helpers;

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
                CustomDesignName = entity.CustomDesign?.DesignName,
                VariantName = entity.ProductVariant == null 
                    ? null 
                    : $"{entity.ProductVariant.Color} - {entity.ProductVariant.Size}"
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
        public static OrderItem CartItemToOrderItem(CartItem cartItem, Guid orderId)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = cartItem.ProductId,
                CustomDesignId = cartItem.CustomDesignId,
                ProductVariantId = cartItem.ProductVariantId,
                ItemName = GetItemName(cartItem),
                SelectedColor = GetSelectedColor(cartItem),
                SelectedSize = GetSelectedSize(cartItem),
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                TotalPrice = cartItem.TotalPrice
            };

            return orderItem;
        }

        public static List<OrderItem> CartItemsToOrderItems(IEnumerable<CartItem> cartItems, Guid orderId)
        {
            return cartItems.Select(ci => CartItemToOrderItem(ci, orderId)).ToList();
        }

        private static string GetItemName(CartItem cartItem)
        {
            if (cartItem.Product != null)
                return cartItem.Product.Name;

            if (cartItem.CustomDesign != null)
                return cartItem.CustomDesign.DesignName;

            return "Unknown Item";
        }

        private static string? GetSelectedColor(CartItem cartItem)
        {
            if (cartItem.ProductVariant != null)
                return cartItem.ProductVariant.Color.ToString();

            return null;
        }

        private static string? GetSelectedSize(CartItem cartItem)
        {
            if (cartItem.ProductVariant != null)
                return cartItem.ProductVariant.Size.ToString();

            return null;
        }
    }
}
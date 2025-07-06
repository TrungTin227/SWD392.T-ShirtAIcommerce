using BusinessObjects.Cart;
using BusinessObjects.Orders;
using DTOs.OrderItem;
using Repositories.Helpers;

namespace Services.Helpers.Mappers
{
    public static class OrderItemMapper
    {
        // --- DTO Mappings ---
        public static OrderItemDto ToDto(OrderItem entity) => new OrderItemDto
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
            VariantName = entity.ProductVariant != null
                ? $"{entity.ProductVariant.Color} - {entity.ProductVariant.Size}"
                : null
        };

        public static IEnumerable<OrderItemDto> ToDtoList(IEnumerable<OrderItem> entities) => entities.Select(ToDto);

        public static PagedList<OrderItemDto> ToPagedDto(PagedList<OrderItem> entities)
        {
            var list = entities.Select(ToDto).ToList();
            return new PagedList<OrderItemDto>(
                list,
                entities.MetaData.TotalCount,
                entities.MetaData.CurrentPage,
                entities.MetaData.PageSize
            );
        }

        // --- Entity Mappings ---
        public static OrderItem ToEntity(CreateOrderItemDto dto, decimal unitPrice)
        {
            var item = new OrderItem
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

            item.TotalPrice = OrderItemBusinessLogic.CalculateTotalPrice(item.UnitPrice, item.Quantity);
            return item;
        }

        public static void UpdateEntity(OrderItem entity, UpdateOrderItemDto dto)
        {
            entity.ItemName = dto.ItemName;
            entity.SelectedColor = dto.SelectedColor;
            entity.SelectedSize = dto.SelectedSize;
            entity.Quantity = dto.Quantity;
            entity.TotalPrice = OrderItemBusinessLogic.CalculateTotalPrice(entity.UnitPrice, entity.Quantity);
        }

        // --- Cart -> Order Mappings ---
        public static OrderItem CartItemToOrderItem(CartItem cartItem, Guid orderId)
        {
            if (cartItem == null) throw new ArgumentNullException(nameof(cartItem));
            if (orderId == Guid.Empty) throw new ArgumentException("OrderId cannot be empty", nameof(orderId));

            return new OrderItem
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
                TotalPrice = CalculateTotalPrice(cartItem.UnitPrice, cartItem.Quantity)
            };
        }

        public static List<OrderItem> CartItemsToOrderItems(IEnumerable<CartItem> cartItems, Guid orderId)
            => cartItems.Select(ci => CartItemToOrderItem(ci, orderId)).ToList();

        // --- Private Helpers ---
        private static string GetItemName(CartItem cartItem)
            => cartItem.ProductVariant?.Product?.Name
               ?? cartItem.Product?.Name
               ?? cartItem.CustomDesign?.DesignName
               ?? "Sản phẩm không xác định";

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


        private static decimal CalculateTotalPrice(decimal unitPrice, int quantity)
        {
            if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));
            return unitPrice * quantity;
        }

        public static OrderItem CartItemToOrderItemWithoutOrderId(CartItem cartItem)
        {
            if (cartItem == null)
                throw new ArgumentNullException(nameof(cartItem));

            // Bắt buộc phải có một trong các ID
            if (!cartItem.ProductId.HasValue
             && !cartItem.CustomDesignId.HasValue
             && !cartItem.ProductVariantId.HasValue)
            {
                throw new InvalidOperationException(
                    "CartItem phải có ít nhất ProductId, CustomDesignId hoặc ProductVariantId");
            }

            // Tạo OrderItem mà không set OrderId (sẽ được set sau)
            var item = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.Empty, // Sẽ được set sau khi tạo Order
                ProductId = cartItem.ProductId,
                CustomDesignId = cartItem.CustomDesignId,
                ProductVariantId = cartItem.ProductVariantId,
                ItemName = GetItemName(cartItem),
                SelectedColor = GetSelectedColor(cartItem),
                SelectedSize = GetSelectedSize(cartItem),
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                TotalPrice = CalculateTotalPrice(cartItem.UnitPrice, cartItem.Quantity)
            };

            return item;
        }
        public static OrderItem CartItemToOrderItemWithValidation(CartItem cartItem, Guid orderId)
        {
            if (cartItem == null)
                throw new ArgumentNullException(nameof(cartItem));
            if (orderId == Guid.Empty)
                throw new ArgumentException("OrderId không được để trống", nameof(orderId));

            // Bắt buộc phải có một trong các ID
            if (!cartItem.ProductId.HasValue
             && !cartItem.CustomDesignId.HasValue
             && !cartItem.ProductVariantId.HasValue)
            {
                throw new InvalidOperationException(
                    "CartItem phải có ít nhất ProductId, CustomDesignId hoặc ProductVariantId");
            }

            // Tạo OrderItem như bình thường
            var item = new OrderItem
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
                TotalPrice = CalculateTotalPrice(cartItem.UnitPrice, cartItem.Quantity)
            };

            return item;
        }
    }
}

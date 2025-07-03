using BusinessObjects.Cart;
using BusinessObjects.CustomDesigns;
using BusinessObjects.Orders;
using BusinessObjects.Products;
using DTOs.OrderItem;

namespace Services.Helpers
{
    public static class OrderItemBusinessLogic
    {
        // Existing methods enhanced
        public static decimal CalculateTotalPrice(decimal unitPrice, int quantity)
        {
            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

            return unitPrice * quantity;
        }

        public static bool ValidateOrderItemInput(CreateOrderItemDto dto)
        {
            // At least one of ProductId, CustomDesignId, or ProductVariantId must be provided
            if (!dto.ProductId.HasValue && !dto.CustomDesignId.HasValue && !dto.ProductVariantId.HasValue)
                return false;

            // ItemName cannot be empty
            if (string.IsNullOrWhiteSpace(dto.ItemName))
                return false;

            // Quantity must be positive
            if (dto.Quantity <= 0)
                return false;

            return true;
        }

        public static bool ValidateColorAndSize(string? color, string? size, Guid? productVariantId)
        {
            // If ProductVariant is specified, color and size should match variant specifications
            // This would require checking against actual variant data
            // For now, basic validation

            if (!string.IsNullOrEmpty(color) && color.Length > 50)
                return false;

            if (!string.IsNullOrEmpty(size) && size.Length > 20)
                return false;

            return true;
        }

        public static decimal GetPriceFromSource(
            Product? product,
            CustomDesign? customDesign,
            ProductVariant? productVariant)
        {
            // Ưu tiên variant: nếu có product gốc thì cộng chênh lệch, 
            // nếu không có chênh lệch thì trả về giá gốc hoặc ném lỗi
            if (productVariant != null)
            {
                if (product != null && productVariant.PriceAdjustment.HasValue)
                    return (product.SalePrice ?? product.Price) + productVariant.PriceAdjustment.Value;

                // Nếu bạn muốn cho phép variant tự định nghĩa giá tuyệt đối
                if (productVariant.PriceAdjustment.HasValue)
                    return productVariant.PriceAdjustment.Value;

                // Không có chênh lệch, quay về giá product (nếu product != null)
                if (product != null)
                    return product.SalePrice ?? product.Price;

                throw new InvalidOperationException("Không xác định được giá từ variant");
            }

            // Nếu không có variant, dùng giá product
            if (product != null)
                return product.SalePrice ?? product.Price;

            // Cuối cùng, nếu là custom design thì dùng TotalPrice
            if (customDesign != null)
                return customDesign.TotalPrice;

            throw new InvalidOperationException("Unable to determine price from any source");
        }

        // Enhanced methods for proper ecommerce order processing

        /// <summary>
        /// Creates order item from cart item with price snapshot
        /// </summary>
        public static OrderItem CreateOrderItemFromCartItem(CartItem cartItem, Order order)
        {
            if (cartItem == null) throw new ArgumentNullException(nameof(cartItem));
            if (order == null) throw new ArgumentNullException(nameof(order));

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                CustomDesignId = cartItem.CustomDesignId,
                ProductVariantId = cartItem.ProductVariantId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice, // Snapshot price at time of order
                TotalPrice = cartItem.TotalPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = order.CreatedBy,
                UpdatedBy = order.CreatedBy
            };

            // Set item name based on source
            if (cartItem.Product != null)
            {
                orderItem.ItemName = cartItem.Product.Name;
                
                // Set color and size from product variant if available
                if (cartItem.ProductVariant != null)
                {
                    orderItem.SelectedColor = cartItem.ProductVariant.Color.ToString();
                    orderItem.SelectedSize = cartItem.ProductVariant.Size.ToString();
                    orderItem.ItemName += $" - {orderItem.SelectedColor} - {orderItem.SelectedSize}";
                }
            }
            else if (cartItem.CustomDesign != null)
            {
                orderItem.ItemName = cartItem.CustomDesign.DesignName ?? "Custom Design";
            }
            else
            {
                orderItem.ItemName = "Unknown Item";
            }

            return orderItem;
        }

        /// <summary>
        /// Validates if cart item can be converted to order item
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateCartItemForOrder(CartItem cartItem)
        {
            if (cartItem == null)
                return (false, "Cart item không tồn tại");

            if (cartItem.Quantity <= 0)
                return (false, "Số lượng phải lớn hơn 0");

            if (cartItem.UnitPrice <= 0)
                return (false, "Giá sản phẩm phải lớn hơn 0");

            // Validate product availability if it's a product-based cart item
            if (cartItem.Product != null)
            {
                if (cartItem.Product.IsDeleted)
                    return (false, $"Sản phẩm '{cartItem.Product.Name}' không còn có sẵn");

                if (cartItem.Product.Status != ProductStatus.Active)
                    return (false, $"Sản phẩm '{cartItem.Product.Name}' hiện không được bán");

                //if (cartItem.Product.Quantity < cartItem.Quantity)
                //    return (false, $"Sản phẩm '{cartItem.Product.Name}' không đủ số lượng trong kho");
            }

            // Validate product variant availability
            if (cartItem.ProductVariant != null)
            {
                if (!cartItem.ProductVariant.IsActive)
                    return (false, "Biến thể sản phẩm không còn có sẵn");

                if (cartItem.ProductVariant.Quantity < cartItem.Quantity)
                    return (false, "Biến thể sản phẩm không đủ số lượng trong kho");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Calculates inventory impact for order item
        /// </summary>
        public static (int ProductQuantityToReduce, int VariantQuantityToReduce) CalculateInventoryImpact(OrderItem orderItem)
        {
            if (orderItem == null)
                return (0, 0);

            var productQuantity = 0;
            var variantQuantity = 0;

            if (orderItem.ProductId.HasValue)
            {
                productQuantity = orderItem.Quantity;
            }

            if (orderItem.ProductVariantId.HasValue)
            {
                variantQuantity = orderItem.Quantity;
            }

            return (productQuantity, variantQuantity);
        }

        /// <summary>
        /// Validates bulk order items creation
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateBulkOrderItems(List<CartItem> cartItems)
        {
            var errors = new List<string>();

            if (!cartItems.Any())
            {
                errors.Add("Danh sách sản phẩm trong giỏ hàng trống");
                return (false, errors);
            }

            var totalItems = cartItems.Sum(x => x.Quantity);
            if (totalItems > 1000) // Business rule: max 1000 items per order
            {
                errors.Add("Tổng số lượng sản phẩm trong đơn hàng không được vượt quá 1000");
            }

            var totalValue = cartItems.Sum(x => x.TotalPrice);
            if (totalValue > 100000000) // Business rule: max 100M VND per order
            {
                errors.Add("Tổng giá trị đơn hàng không được vượt quá 100,000,000 VND");
            }

            foreach (var cartItem in cartItems)
            {
                var validation = ValidateCartItemForOrder(cartItem);
                if (!validation.IsValid)
                {
                    errors.Add(validation.ErrorMessage);
                }
            }

            return (!errors.Any(), errors);
        }

        /// <summary>
        /// Calculates order totals from order items
        /// </summary>
        public static (decimal Subtotal, decimal Tax, decimal Total) CalculateOrderTotals(
            List<OrderItem> orderItems, 
            decimal taxRate = 0.1m,
            decimal shippingFee = 0m,
            decimal discountAmount = 0m)
        {
            var subtotal = orderItems.Sum(x => x.TotalPrice);
            var tax = Math.Round(subtotal * taxRate, 2);
            var total = subtotal + tax + shippingFee - discountAmount;

            return (subtotal, tax, Math.Max(0, total));
        }

        /// <summary>
        /// Validates order item update
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateOrderItemUpdate(
            OrderItem orderItem,
            OrderStatus currentOrderStatus)
        {
            if (orderItem == null)
                return (false, "Order item không tồn tại");

            // Only allow updates for pending orders
            if (currentOrderStatus != OrderStatus.Pending)
                return (false, "Chỉ có thể chỉnh sửa đơn hàng ở trạng thái chờ xử lý");

            if (orderItem.Quantity <= 0)
                return (false, "Số lượng phải lớn hơn 0");

            if (orderItem.UnitPrice <= 0)
                return (false, "Giá sản phẩm phải lớn hơn 0");

            return (true, string.Empty);
        }

        /// <summary>
        /// Creates order item snapshot for historical tracking
        /// </summary>
        public static OrderItemSnapshot CreateOrderItemSnapshot(OrderItem orderItem)
        {
            return new OrderItemSnapshot
            {
                OrderItemId = orderItem.Id,
                ItemName = orderItem.ItemName,
                SelectedColor = orderItem.SelectedColor,
                SelectedSize = orderItem.SelectedSize,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.TotalPrice,
                SnapshotDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Generates order item display name
        /// </summary>
        public static string GenerateDisplayName(OrderItem orderItem)
        {
            if (orderItem == null)
                return "Unknown Item";

            var name = orderItem.ItemName;

            if (!string.IsNullOrEmpty(orderItem.SelectedColor))
                name += $" - {orderItem.SelectedColor}";

            if (!string.IsNullOrEmpty(orderItem.SelectedSize))
                name += $" - {orderItem.SelectedSize}";

            return name;
        }
    }

    // Supporting class for order item snapshots
    public class OrderItemSnapshot
    {
        public Guid OrderItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime SnapshotDate { get; set; }
    }
}
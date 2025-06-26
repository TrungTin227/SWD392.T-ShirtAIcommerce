using BusinessObjects.CustomDesigns;
using BusinessObjects.Products;
using DTOs.OrderItem;

namespace Services.Helpers
{
    public static class OrderItemBusinessLogic
    {
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
                    return product.Price + productVariant.PriceAdjustment.Value;

                // Nếu bạn muốn cho phép variant tự định nghĩa giá tuyệt đối
                if (productVariant.PriceAdjustment.HasValue)
                    return productVariant.PriceAdjustment.Value;

                // Không có chênh lệch, quay về giá product (nếu product != null)
                if (product != null)
                    return product.Price;

                throw new InvalidOperationException("Không xác định được giá từ variant");
            }

            // Nếu không có variant, dùng giá product
            if (product != null)
                return product.Price;

            // Cuối cùng, nếu là custom design thì dùng TotalPrice
            if (customDesign != null)
                return customDesign.TotalPrice;

            throw new InvalidOperationException("Unable to determine price from any source");
        }

    }
}
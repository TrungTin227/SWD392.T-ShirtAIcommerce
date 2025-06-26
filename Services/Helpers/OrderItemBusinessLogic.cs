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
            BusinessObjects.Products.Product? product,
            BusinessObjects.CustomDesigns.CustomDesign? customDesign,
            BusinessObjects.Products.ProductVariant? productVariant)
        {
            // Priority: ProductVariant > Product > CustomDesign
            if (productVariant != null && productVariant.Price.HasValue)
                return productVariant.Price.Value;

            if (product != null)
                return product.Price;

            if (customDesign != null)
                return customDesign.Price;

            throw new InvalidOperationException("Unable to determine price from any source");
        }
    }
}
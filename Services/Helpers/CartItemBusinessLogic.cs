namespace Services.Helpers
{
    public static class CartItemBusinessLogic
    {
        public static decimal CalculateTotalPrice(decimal unitPrice, int quantity)
        {
            if (unitPrice < 0 || quantity < 0)
                throw new ArgumentException("Unit price và quantity phải lớn hơn hoặc bằng 0");

            return unitPrice * quantity;
        }

        public static bool ValidateCartItemData(Guid? productId, Guid? customDesignId, Guid? productVariantId)
        {
            // Ít nhất một trong các ID phải có giá trị
            return productId.HasValue || customDesignId.HasValue || productVariantId.HasValue;
        }

        public static bool ValidateUserOrSession(Guid? userId, string? sessionId)
        {
            // Phải có ít nhất userId hoặc sessionId
            return userId.HasValue || !string.IsNullOrEmpty(sessionId);
        }

        public static string GenerateCartItemKey(Guid? productId, Guid? customDesignId, Guid? productVariantId, string? selectedColor, string? selectedSize)
        {
            return $"{productId}_{customDesignId}_{productVariantId}_{selectedColor}_{selectedSize}";
        }

        public static bool IsValidQuantity(int quantity)
        {
            return quantity >= 1 && quantity <= 100;
        }

        public static bool IsValidUnitPrice(decimal unitPrice)
        {
            return unitPrice > 0 && unitPrice <= 10000000; // Max 10 triệu VND
        }
    }
}
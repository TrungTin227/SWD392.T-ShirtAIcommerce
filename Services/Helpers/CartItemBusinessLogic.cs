using BusinessObjects.Products;

namespace Services.Helpers
{
    public static class CartItemBusinessLogic
    {
        // Existing methods enhanced
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

        // Enhanced methods for proper ecommerce logic
        
        /// <summary>
        /// Validates if requested quantity is available in stock
        /// </summary>
        public static bool ValidateStockAvailability(int requestedQuantity, int availableStock)
        {
            return availableStock >= requestedQuantity && requestedQuantity > 0;
        }

        /// <summary>
        /// Checks if product is available for purchase
        /// </summary>
        public static bool IsProductAvailable(ProductStatus status, bool isActive = true)
        {
            return status == ProductStatus.Active && isActive;
        }

        /// <summary>
        /// Validates price consistency for cart items
        /// </summary>
        public static bool ValidatePriceConsistency(decimal cartPrice, decimal currentPrice, decimal tolerancePercentage = 0.01m)
        {
            if (currentPrice == 0) return false;
            
            var difference = Math.Abs(cartPrice - currentPrice);
            var tolerance = currentPrice * tolerancePercentage;
            
            return difference <= tolerance;
        }

        /// <summary>
        /// Calculates maximum allowed quantity based on stock and limits
        /// </summary>
        public static int CalculateMaxAllowedQuantity(int availableStock, int maxOrderQuantity, int currentCartQuantity = 0)
        {
            var stockLimit = Math.Max(0, availableStock - currentCartQuantity);
            return Math.Min(stockLimit, maxOrderQuantity);
        }

        /// <summary>
        /// Validates if cart item can be added with specified quantity
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateAddToCart(
            int requestedQuantity, 
            int availableStock, 
            int maxOrderQuantity, 
            int minOrderQuantity,
            int currentCartQuantity = 0)
        {
            if (requestedQuantity < minOrderQuantity)
                return (false, $"Số lượng tối thiểu là {minOrderQuantity}");

            if (requestedQuantity > maxOrderQuantity)
                return (false, $"Số lượng tối đa là {maxOrderQuantity}");

            var totalQuantity = currentCartQuantity + requestedQuantity;
            if (totalQuantity > availableStock)
                return (false, $"Không đủ hàng trong kho. Còn lại: {Math.Max(0, availableStock - currentCartQuantity)}");

            if (totalQuantity > maxOrderQuantity)
                return (false, $"Tổng số lượng vượt quá giới hạn đặt hàng: {maxOrderQuantity}");

            return (true, string.Empty);
        }

        /// <summary>
        /// Determines if cart should expire based on last update time
        /// </summary>
        public static bool ShouldExpireCart(DateTime lastUpdated, TimeSpan expirationTime)
        {
            return DateTime.UtcNow - lastUpdated > expirationTime;
        }

        /// <summary>
        /// Calculates cart expiration time
        /// </summary>
        public static DateTime CalculateCartExpiration(DateTime lastUpdated, TimeSpan expirationTime)
        {
            return lastUpdated.Add(expirationTime);
        }

        /// <summary>
        /// Validates bulk cart operations
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateBulkCartItems(List<(Guid Id, int Quantity)> items)
        {
            var errors = new List<string>();
            
            if (!items.Any())
            {
                errors.Add("Danh sách sản phẩm không được rỗng");
                return (false, errors);
            }

            if (items.Count > 50) // Limit bulk operations
            {
                errors.Add("Không thể xử lý quá 50 sản phẩm cùng lúc");
            }

            var duplicateIds = items.GroupBy(x => x.Id)
                                  .Where(g => g.Count() > 1)
                                  .Select(g => g.Key);
            
            if (duplicateIds.Any())
            {
                errors.Add("Có sản phẩm bị trùng lặp trong danh sách");
            }

            foreach (var item in items)
            {
                if (!IsValidQuantity(item.Quantity))
                {
                    errors.Add($"Số lượng không hợp lệ cho sản phẩm {item.Id}");
                }
            }

            return (!errors.Any(), errors);
        }

        /// <summary>
        /// Generates session ID for guest users
        /// </summary>
        public static string GenerateGuestSessionId()
        {
            return $"guest_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// Validates session ID format
        /// </summary>
        public static bool IsValidSessionId(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            // Should be at least 10 characters and not contain invalid characters
            return sessionId.Length >= 10 && 
                   sessionId.Length <= 255 && 
                   !sessionId.Contains('\0') &&
                   !sessionId.Contains('\n') &&
                   !sessionId.Contains('\r');
        }
    }
}
using BusinessObjects.Products;

namespace Services.Helpers
{
    public static class ProductBusinessLogic
    {
        /// <summary>
        /// Validates if product is available for purchase
        /// </summary>
        public static bool IsProductAvailable(Product product)
        {
            return product != null &&
                   !product.IsDeleted &&
                   product.Status == ProductStatus.Active &&
                   product.Quantity > 0;
        }

        /// <summary>
        /// Validates if product variant is available for purchase
        /// </summary>
        public static bool IsProductVariantAvailable(ProductVariant variant)
        {
            return variant != null &&
                   variant.IsActive &&
                   variant.Quantity > 0;
        }

        /// <summary>
        /// Gets effective price for a product (sale price if available, otherwise regular price)
        /// </summary>
        public static decimal GetEffectivePrice(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            return product.SalePrice ?? product.Price;
        }

        /// <summary>
        /// Gets effective price for a product variant
        /// </summary>
        public static decimal GetEffectivePrice(Product product, ProductVariant variant)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            var basePrice = GetEffectivePrice(product);
            return basePrice + (variant.PriceAdjustment ?? 0);
        }

        /// <summary>
        /// Calculates available stock for a product
        /// </summary>
        public static int GetAvailableStock(Product product)
        {
            if (product == null || !IsProductAvailable(product))
                return 0;

            return Math.Max(0, product.Quantity);
        }

        /// <summary>
        /// Calculates available stock for a product variant
        /// </summary>
        public static int GetAvailableStock(ProductVariant variant)
        {
            if (variant == null || !IsProductVariantAvailable(variant))
                return 0;

            return Math.Max(0, variant.Quantity);
        }

        /// <summary>
        /// Validates quantity limits for a product
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateQuantityLimits(
            Product product, 
            int requestedQuantity)
        {
            if (product == null)
                return (false, "Sản phẩm không tồn tại");

            if (requestedQuantity < product.MinOrderQuantity)
                return (false, $"Số lượng đặt hàng tối thiểu là {product.MinOrderQuantity}");

            if (requestedQuantity > product.MaxOrderQuantity)
                return (false, $"Số lượng đặt hàng tối đa là {product.MaxOrderQuantity}");

            var availableStock = GetAvailableStock(product);
            if (requestedQuantity > availableStock)
                return (false, $"Không đủ hàng trong kho. Còn lại: {availableStock}");

            return (true, string.Empty);
        }

        /// <summary>
        /// Validates if product can be ordered with specific color and size
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateProductVariant(
            Product product,
            ProductColor? color,
            ProductSize? size)
        {
            if (product == null)
                return (false, "Sản phẩm không tồn tại");

            if (color.HasValue && !product.AvailableColors.Contains(color.Value))
                return (false, $"Màu {color.Value} không có sẵn cho sản phẩm này");

            if (size.HasValue && !product.AvailableSizes.Contains(size.Value))
                return (false, $"Size {size.Value} không có sẵn cho sản phẩm này");

            return (true, string.Empty);
        }

        /// <summary>
        /// Calculates discount amount for a product
        /// </summary>
        public static decimal CalculateDiscountAmount(Product product)
        {
            if (product?.SalePrice == null || product.SalePrice >= product.Price)
                return 0;

            return product.Price - product.SalePrice.Value;
        }

        /// <summary>
        /// Calculates discount percentage for a product
        /// </summary>
        public static decimal CalculateDiscountPercentage(Product product)
        {
            if (product?.SalePrice == null || product.SalePrice >= product.Price || product.Price == 0)
                return 0;

            return Math.Round(((product.Price - product.SalePrice.Value) / product.Price) * 100, 2);
        }

        /// <summary>
        /// Checks if product has active discount
        /// </summary>
        public static bool HasActiveDiscount(Product product)
        {
            return product?.SalePrice != null && product.SalePrice < product.Price;
        }

        /// <summary>
        /// Updates inventory after order placement
        /// </summary>
        public static (bool Success, string ErrorMessage) ReserveInventory(
            Product product, 
            int quantity)
        {
            if (product == null)
                return (false, "Sản phẩm không tồn tại");

            if (product.Quantity < quantity)
                return (false, "Không đủ hàng trong kho");

            // This would typically be handled in the repository/service layer
            // but we provide the business logic here
            return (true, string.Empty);
        }

        /// <summary>
        /// Updates inventory for product variant after order placement
        /// </summary>
        public static (bool Success, string ErrorMessage) ReserveInventory(
            ProductVariant variant, 
            int quantity)
        {
            if (variant == null)
                return (false, "Biến thể sản phẩm không tồn tại");

            if (variant.Quantity < quantity)
                return (false, "Không đủ hàng trong kho");

            return (true, string.Empty);
        }

        /// <summary>
        /// Restores inventory after order cancellation
        /// </summary>
        public static void RestoreInventory(Product product, int quantity)
        {
            // Business logic for inventory restoration
            // Actual implementation would be in repository layer
        }

        /// <summary>
        /// Restores inventory for product variant after order cancellation
        /// </summary>
        public static void RestoreInventory(ProductVariant variant, int quantity)
        {
            // Business logic for inventory restoration
            // Actual implementation would be in repository layer
        }

        /// <summary>
        /// Validates product data for creation/update
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateProductData(Product product)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(product?.Name))
                errors.Add("Tên sản phẩm là bắt buộc");

            if (product?.Price <= 0)
                errors.Add("Giá sản phẩm phải lớn hơn 0");

            if (product?.SalePrice != null && product.SalePrice >= product.Price)
                errors.Add("Giá khuyến mãi phải nhỏ hơn giá gốc");

            if (product?.Quantity < 0)
                errors.Add("Số lượng không được âm");

            if (product?.MinOrderQuantity < 1)
                errors.Add("Số lượng đặt hàng tối thiểu phải >= 1");

            if (product?.MaxOrderQuantity < product?.MinOrderQuantity)
                errors.Add("Số lượng đặt hàng tối đa phải >= số lượng tối thiểu");

            if (product?.Weight <= 0)
                errors.Add("Trọng lượng phải lớn hơn 0");

            return (!errors.Any(), errors);
        }

        /// <summary>
        /// Generates SKU for product
        /// </summary>
        public static string GenerateSku(string productName, string? categoryCode = null)
        {
            var cleanName = productName.Replace(" ", "").ToUpperInvariant();
            var namePart = cleanName.Length > 6 ? cleanName.Substring(0, 6) : cleanName.PadRight(6, 'X');
            var categoryPart = string.IsNullOrEmpty(categoryCode) ? "GEN" : categoryCode.ToUpperInvariant();
            var timePart = DateTime.UtcNow.ToString("MMdd");
            var randomPart = new Random().Next(100, 999);

            return $"{categoryPart}-{namePart}-{timePart}-{randomPart}";
        }

        /// <summary>
        /// Generates slug for SEO
        /// </summary>
        public static string GenerateSlug(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return string.Empty;

            return productName.ToLowerInvariant()
                              .Replace(" ", "-")
                              .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                              .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                              .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                              .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                              .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                              .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                              .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                              .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                              .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                              .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                              .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                              .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y")
                              .Replace("đ", "d")
                              .Replace("--", "-")
                              .Trim('-');
        }
    }
}
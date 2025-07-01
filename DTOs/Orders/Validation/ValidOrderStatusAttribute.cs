using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Orders.Validation
{
    public class ValidOrderStatusAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null) return false;
            return Enum.IsDefined(typeof(OrderStatus), value);
        }

        public override string FormatErrorMessage(string name)
        {
            var validStatuses = string.Join(", ", Enum.GetNames(typeof(OrderStatus)));
            return $"Trạng thái {name} không hợp lệ. Các trạng thái hợp lệ: {validStatuses}";
        }
    }

    public class ValidPaymentStatusAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null) return false;
            return Enum.IsDefined(typeof(PaymentStatus), value);
        }

        public override string FormatErrorMessage(string name)
        {
            var validStatuses = string.Join(", ", Enum.GetNames(typeof(PaymentStatus)));
            return $"Trạng thái thanh toán {name} không hợp lệ. Các trạng thái hợp lệ: {validStatuses}";
        }
    }

    public class AtLeastOneProductAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not CreateOrderItemRequest item) return false;

            return item.ProductId.HasValue || item.CustomDesignId.HasValue || item.ProductVariantId.HasValue;
        }

        public override string FormatErrorMessage(string name)
        {
            return "Mỗi sản phẩm trong đơn hàng phải có ít nhất một trong các ID: ProductId, CustomDesignId, hoặc ProductVariantId";
        }
    }

    public class ValidAddressSelectionAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not CreateOrderRequest request) return false;

            // Either UserAddressId or NewAddress should be provided, but not both
            var hasUserAddressId = request.UserAddressId.HasValue;
            var hasNewAddress = request.NewAddress != null;

            // If neither is provided, it's still valid as the service will try to use default address
            if (!hasUserAddressId && !hasNewAddress) return true;

            // If both are provided, it's invalid
            if (hasUserAddressId && hasNewAddress) return false;

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return "Chỉ có thể chọn một địa chỉ có sẵn HOẶC tạo địa chỉ mới, không thể chọn cả hai";
        }
    }

    public class ValidOrderItemsAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not IList<CreateOrderItemRequest> items) return false;

            if (!items.Any()) return false;

            foreach (var item in items)
            {
                // Each item must have at least one product reference
                if (!item.ProductId.HasValue && !item.CustomDesignId.HasValue && !item.ProductVariantId.HasValue)
                    return false;

                // If not from cart, must have required fields
                if (!item.CartItemId.HasValue)
                {
                    if (!item.UnitPrice.HasValue || item.UnitPrice <= 0) return false;
                    if (!item.Quantity.HasValue || item.Quantity <= 0) return false;
                    if (string.IsNullOrWhiteSpace(item.ItemName)) return false;
                }
            }

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return "Danh sách sản phẩm không hợp lệ. Mỗi sản phẩm phải có đầy đủ thông tin bắt buộc";
        }
    }
}
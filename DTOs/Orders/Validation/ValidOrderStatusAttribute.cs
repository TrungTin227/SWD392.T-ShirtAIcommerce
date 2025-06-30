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
}
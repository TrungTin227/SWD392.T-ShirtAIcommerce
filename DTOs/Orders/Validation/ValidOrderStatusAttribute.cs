using System.ComponentModel.DataAnnotations;

namespace DTOs.Orders.Validation
{
    public class ValidOrderStatusAttribute : ValidationAttribute
    {
        private readonly string[] _validStatuses = { "Pending", "Confirmed", "Processing", "Shipping", "Delivered", "Cancelled", "Returned" };

        public override bool IsValid(object? value)
        {
            if (value == null) return false;
            return _validStatuses.Contains(value.ToString());
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Trạng thái {name} không hợp lệ. Các trạng thái hợp lệ: {string.Join(", ", _validStatuses)}";
        }
    }

    public class ValidPaymentStatusAttribute : ValidationAttribute
    {
        private readonly string[] _validStatuses = { "Unpaid", "Paid", "PartiallyPaid", "Refunded", "PartiallyRefunded" };

        public override bool IsValid(object? value)
        {
            if (value == null) return false;
            return _validStatuses.Contains(value.ToString());
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Trạng thái thanh toán {name} không hợp lệ. Các trạng thái hợp lệ: {string.Join(", ", _validStatuses)}";
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
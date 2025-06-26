using System.ComponentModel.DataAnnotations;

namespace DTOs.Orders
{
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại người nhận là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? CustomerNotes { get; set; }

        public Guid? CouponId { get; set; }
        public Guid? ShippingMethodId { get; set; }

        [Required(ErrorMessage = "Sản phẩm trong đơn hàng là bắt buộc")]
        [MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất 1 sản phẩm")]
        public List<CreateOrderItemRequest> OrderItems { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tên sản phẩm không được vượt quá 255 ký tự")]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Màu sắc không được vượt quá 50 ký tự")]
        public string? SelectedColor { get; set; }

        [MaxLength(20, ErrorMessage = "Kích thước không được vượt quá 20 ký tự")]
        public string? SelectedSize { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal UnitPrice { get; set; }
    }
}
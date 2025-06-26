using System.ComponentModel.DataAnnotations;

namespace DTOs.Orders
{
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "ID người dùng là bắt buộc")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [MaxLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại người nhận là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20)]
        public string ReceiverPhone { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? CustomerNotes { get; set; }

        public Guid? CouponId { get; set; }
        public Guid? ShippingMethodId { get; set; }

        [Required(ErrorMessage = "Sản phẩm trong đơn hàng là bắt buộc")]
        [MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất 1 sản phẩm")]
        public List<CreateOrderItemRequest> OrderItems { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        [Required(ErrorMessage = "ID sản phẩm là bắt buộc")]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal UnitPrice { get; set; }

        public Guid? ProductVariantId { get; set; }
    }
}
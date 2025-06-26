using System.ComponentModel.DataAnnotations;

namespace DTOs.Orders
{
    public class UpdateOrderRequest
    {
        [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? ShippingAddress { get; set; }

        [MaxLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự")]
        public string? ReceiverName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? ReceiverPhone { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? CustomerNotes { get; set; }

        public Guid? CouponId { get; set; }
        public Guid? ShippingMethodId { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }

        [MaxLength(100, ErrorMessage = "Mã vận đơn không được vượt quá 100 ký tự")]
        public string? TrackingNumber { get; set; }

        public Guid? AssignedStaffId { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái đơn hàng là bắt buộc")]
        public string Status { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự")]
        public string? Reason { get; set; }
    }

    public class UpdatePaymentStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái thanh toán là bắt buộc")]
        public string PaymentStatus { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }
    }

    public class AssignStaffRequest
    {
        [Required(ErrorMessage = "ID nhân viên là bắt buộc")]
        public Guid StaffId { get; set; }

        [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }
    }

    public class CancelOrderRequest
    {
        [Required(ErrorMessage = "Lý do hủy đơn hàng là bắt buộc")]
        [MaxLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }

    public class BulkUpdateStatusRequest
    {
        [Required(ErrorMessage = "Danh sách ID đơn hàng là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 đơn hàng")]
        public List<Guid> OrderIds { get; set; } = new();

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public string Status { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }
    }
}
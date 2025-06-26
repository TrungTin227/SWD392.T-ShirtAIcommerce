using BusinessObjects.Orders;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Orders
{
    public class UpdateOrderRequest
    {
        public OrderStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [MaxLength(100)]
        public string? ReceiverName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20)]
        public string? ReceiverPhone { get; set; }

        [MaxLength(1000)]
        public string? CustomerNotes { get; set; }

        public DateTime? EstimatedDeliveryDate { get; set; }

        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public Guid? AssignedStaffId { get; set; }
        public Guid? ShippingMethodId { get; set; }
    }
}
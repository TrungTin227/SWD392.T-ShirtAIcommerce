using DTOs.OrderItem;

namespace DTOs.Orders
{
    public class OrderDTO
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CancellationReason { get; set; }
        public Guid? AssignedStaffId { get; set; }
        public Guid? CouponId { get; set; }
        public Guid? ShippingMethodId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        // Navigation properties
        public string UserName { get; set; } = string.Empty;
        public string? AssignedStaffName { get; set; }
        public string? CouponCode { get; set; }
        public string? ShippingMethodName { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
        public decimal FinalTotal => TotalAmount + ShippingFee + TaxAmount - DiscountAmount;
    }
}
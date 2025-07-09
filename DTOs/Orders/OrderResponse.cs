using BusinessObjects.Products;
using DTOs.OrderItem;

namespace DTOs.Orders
{
    /// <summary>
    /// Response DTO for Order operations - contains all order information
    /// </summary>
    public class OrderResponse
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
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
        public string UserEmail { get; set; } = string.Empty;
        public string? AssignedStaffName { get; set; }
        public string? CouponCode { get; set; }
        public string? ShippingMethodName { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
        
        // Calculated properties
        public decimal FinalTotal => TotalAmount + ShippingFee - DiscountAmount;
        public decimal SubTotal => TotalAmount - ShippingFee  + DiscountAmount;
        public bool IsEditable => Status == OrderStatus.Pending || Status == OrderStatus.Processing;
        public bool IsCancellable => Status != OrderStatus.Delivered && Status != OrderStatus.Cancelled;
    }

    /// <summary>
    /// Simplified order response for lists
    /// </summary>
    public class OrderSummaryResponse
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal FinalTotal { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ItemCount { get; set; }
        public string? TrackingNumber { get; set; }
    }
}
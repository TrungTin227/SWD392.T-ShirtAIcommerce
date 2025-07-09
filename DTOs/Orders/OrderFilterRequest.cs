using BusinessObjects.Common;

namespace DTOs.Orders
{
    public class OrderFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;

        // Filter fields
        public Guid? UserId { get; set; }
        public OrderStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public Guid? AssignedStaffId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? OrderNumber { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? Search { get; set; }
        public bool? HasTracking { get; set; }
        public Guid? CouponId { get; set; }
        public Guid? ShippingMethodId { get; set; }
    }

    /// <summary>
    /// Alias for OrderFilterRequest for backward compatibility
    /// </summary>
    public class OrderFilterDto : OrderFilterRequest
    {
    }
}
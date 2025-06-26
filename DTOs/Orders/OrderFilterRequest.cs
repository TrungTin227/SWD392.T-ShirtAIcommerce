using BusinessObjects.Orders;
using DTOs.Common;

namespace DTOs.Orders
{
    public class OrderFilterRequest : PaginationRequest
    {
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
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
    }
}
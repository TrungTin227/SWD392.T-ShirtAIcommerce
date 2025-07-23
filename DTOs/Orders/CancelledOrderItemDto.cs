namespace DTOs.Orders
{
    /// <summary>
    /// Đại diện cho thông tin tóm tắt của một sản phẩm trong đơn hàng đã hủy.
    /// </summary>
    public class CancelledOrderItemDto
    {
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Đại diện cho thông tin chi tiết của một đơn hàng đã được hủy,
    /// được trả về cho client.
    /// </summary>
    public class CancelledOrderDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Cancelled";

        // Thông tin quan trọng về việc hủy đơn
        public string? CancellationReason { get; set; }
        public DateTime? DateCancelled { get; set; } // Dùng UpdatedAt làm ngày hủy
        public string? AdminReviewNotes { get; set; }

        public List<CancelledOrderItemDto> Items { get; set; } = new List<CancelledOrderItemDto>();
    }
}

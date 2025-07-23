using BusinessObjects.Common;

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
        public string? VariantName { get; set; }
        public string? VariantImageUrl { get; set; }
    }

    /// <summary>
    /// Đại diện cho thông tin chi tiết của một đơn hàng đã được hủy,
    /// được trả về cho client.
    /// </summary>
    public class CancelledOrderDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        // --- Thông tin người nhận (Thêm mới) ---
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string RefundAmount => TotalAmount.ToString("0.##");
        public decimal SubtotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // --- Trạng thái ---
        public string Status { get; set; } = "Cancelled";
        public CancellationRequestStatus CancellationRequestStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        // --- Thông tin về việc hủy đơn ---
        public string? CancellationReason { get; set; }
        public DateTime? DateCancelled { get; set; } // Dùng UpdatedAt làm ngày hủy
        public string? AdminReviewNotes { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public List<CancelledOrderItemDto> Items { get; set; } = new List<CancelledOrderItemDto>();
    }
}

namespace DTOs.Orders
{
    public class CreateOrderFromCartRequest
    {
        public string? SessionId { get; set; }
        public Guid? ShippingAddressId { get; set; }
        public Guid? ShippingMethodId { get; set; }
        public Guid? CouponId { get; set; }
        public string? Notes { get; set; }
        public List<Guid>? SelectedCartItemIds { get; set; } // Nếu null thì lấy tất cả items
    }

    public class OrderValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public decimal EstimatedTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public int TotalItems { get; set; }
        public List<OrderItemValidationDto> Items { get; set; } = new();
    }

    public class OrderItemValidationDto
    {
        public Guid CartItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariantInfo { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsAvailable { get; set; }
        public bool HasStockIssue { get; set; }
        public bool HasPriceChange { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class OrderAnalyticsDto
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItems { get; set; }
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();
        public Dictionary<string, decimal> RevenueByStatus { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<DailyOrderStatsDto> DailyStats { get; set; } = new();
        public OrderTrendsDto Trends { get; set; } = new();
    }

    public class TopProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DailyOrderStatsDto
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public int ItemsSold { get; set; }
    }

    public class OrderTrendsDto
    {
        public decimal RevenueGrowth { get; set; } // Percentage
        public decimal OrderCountGrowth { get; set; } // Percentage
        public decimal AverageOrderValueGrowth { get; set; } // Percentage
        public string TrendDirection { get; set; } = "Stable"; // "Growing", "Declining", "Stable"
    }

    public class BulkCancelOrdersRequest
    {
        public List<Guid> OrderIds { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }
}
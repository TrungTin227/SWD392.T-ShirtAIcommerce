namespace DTOs.Cart
{
    public class CartAnalyticsDto
    {
        public int TotalItems { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageItemPrice { get; set; }
        public int UniqueProducts { get; set; }
        public DateTime? LastUpdated { get; set; }
        public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
        public List<PriceRangeDto> PriceRanges { get; set; } = new();
        public CartHealthDto Health { get; set; } = new();
    }

    public class CategoryBreakdownDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal Percentage { get; set; }
    }

    public class PriceRangeDto
    {
        public string Range { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class CartHealthDto
    {
        public bool HasExpiredItems { get; set; }
        public bool HasUnavailableItems { get; set; }
        public bool HasPriceChanges { get; set; }
        public int IssueCount { get; set; }
        public List<string> Issues { get; set; } = new();
        public decimal HealthScore { get; set; } // 0-100
    }
}
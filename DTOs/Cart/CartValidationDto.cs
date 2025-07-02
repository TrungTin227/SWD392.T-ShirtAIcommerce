namespace DTOs.Cart
{
    public class CartValidationDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<CartItemValidationDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
        public bool HasPriceChanges { get; set; }
        public bool HasStockIssues => Items.Any(x => x.HasStockIssue);
        public bool HasUnavailableItems => Items.Any(x => !x.IsAvailable);
        public string Summary { get; set; } = string.Empty;
    }

    public class CartItemValidationDto
    {
        public Guid CartItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariantInfo { get; set; } 
        public bool IsAvailable { get; set; }
        public bool HasStockIssue { get; set; }
        public bool HasPriceChange { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal CartPrice { get; set; }
        public decimal PriceDifference { get; set; } 
        public int Quantity { get; set; } 
        public int AvailableQuantity { get; set; } 
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; } 
    }
}

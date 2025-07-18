namespace DTOs.OrderItem
{
    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // Additional info from navigation properties
        public string? ProductName { get; set; }
        public string? CustomDesignName { get; set; }
        public string? VariantName { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CreateOrderItemDto
    {
        public Guid OrderId { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateOrderItemDto
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderItemQueryDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortBy { get; set; } = "Id";
        public bool IsDescending { get; set; } = true;
        public Guid? OrderId { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
        public decimal? MinUnitPrice { get; set; }
        public decimal? MaxUnitPrice { get; set; }
        public int? MinQuantity { get; set; }
        public int? MaxQuantity { get; set; }
    }
}
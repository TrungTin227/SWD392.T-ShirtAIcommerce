namespace DTOs.Cart
{
    public class CartItemQueryDto
    {
        // === Pagination Properties (theo pattern OrderFilterRequest) ===
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;

        // === Filter Properties ===
        public Guid? UserId { get; set; }
        public string? SessionId { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
        public int? MinQuantity { get; set; }
        public int? MaxQuantity { get; set; }
        public decimal? MinUnitPrice { get; set; }
        public decimal? MaxUnitPrice { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }

        /// <summary>
        /// If true, sort descending; otherwise, ascending.
        /// </summary>
        public bool IsDescending { get; set; } = false;
    }
}
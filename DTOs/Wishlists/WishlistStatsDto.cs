namespace DTOs.Wishlists
{
    public class WishlistStatsDto
    {
        public int TotalWishlistItems { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueProducts { get; set; }
        public decimal TotalWishlistValue { get; set; }
        public List<ProductWishlistStatsDto> TopWishlistedProducts { get; set; } = new();
        public Dictionary<DateTime, int> WishlistTrends { get; set; } = new();
    }

    public class ProductWishlistStatsDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public int WishlistCount { get; set; }
        public decimal ProductPrice { get; set; }
    }

    public class UserWishlistSummaryDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalWishlistItems { get; set; }
        public decimal TotalWishlistValue { get; set; }
        public DateTime LastWishlistActivity { get; set; }
        public List<WishlistItemDto> RecentItems { get; set; } = new();
    }
}
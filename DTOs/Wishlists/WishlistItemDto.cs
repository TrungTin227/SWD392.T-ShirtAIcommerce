namespace DTOs.Wishlists
{
    public class WishlistItemDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal ProductPrice { get; set; }
        public string? ProductDescription { get; set; }
        public bool IsProductAvailable { get; set; }
        public int ProductStock { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
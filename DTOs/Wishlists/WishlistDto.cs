using System.ComponentModel.DataAnnotations;

namespace DTOs.Wishlists
{
    public class AddToWishlistDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public Guid ProductId { get; set; }
    }

    public class WishlistFilterDto
    {
        public Guid? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsProductAvailable { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string OrderBy { get; set; } = "CreatedAt";
        public bool OrderByDescending { get; set; } = true;
    }
}
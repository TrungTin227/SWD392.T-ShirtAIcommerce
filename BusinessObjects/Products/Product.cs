using BusinessObjects.Cart;
using BusinessObjects.Identity;
using BusinessObjects.Orders;
using BusinessObjects.Reviews;
using BusinessObjects.Wishlists;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BusinessObjects.Products
{
    public class Product : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        [Required]
        public ProductMaterial Material { get; set; }
        [Required]
        public ProductSeason Season { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required, Column(TypeName = "decimal(12,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? SalePrice { get; set; }

        [MaxLength(100)]
        public string? Sku { get; set; }

        [MaxLength(255)]
        public string? Slug { get; set; }

        [MaxLength(160)]
        public string? MetaTitle { get; set; }

        [MaxLength(320)]
        public string? MetaDescription { get; set; }

        [Required]
        public ProductStatus Status { get; set; } = ProductStatus.Active;

        // Navigation properties
        public Guid? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // Relations to other modules
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    }
}
using BusinessObjects.Cart;
using BusinessObjects.Identity;
using BusinessObjects.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Products
{
    public class ProductVariant : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }

        [Required]
        public ProductColor Color { get; set; }

        [Required]
        public ProductSize Size { get; set; }

        [MaxLength(100)]
        public string? VariantSku { get; set; }

        public int Quantity { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal? PriceAdjustment { get; set; } = 0m;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation back to parent product
        public virtual Product Product { get; set; } = null!;

        // Relations to cart and orders
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
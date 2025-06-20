using BusinessObjects.CustomDesigns;
using BusinessObjects.Identity;
using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Cart
{
    public class CartItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }

        [MaxLength(255)]
        public string? SessionId { get; set; } // For guest users

        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }

        [MaxLength(50)]
        public string? SelectedColor { get; set; }

        [MaxLength(20)]
        public string? SelectedSize { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng từ 1-100")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Đơn giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 0")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalPrice => UnitPrice * Quantity;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("CustomDesignId")]
        public virtual CustomDesign? CustomDesign { get; set; }

        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }
    }
}
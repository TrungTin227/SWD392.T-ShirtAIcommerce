using BusinessObjects.CustomDesigns;
using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Orders
{
    // KHÔNG kế thừa BaseEntity - dữ liệu snapshot, không cần tracking
    public class OrderItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? SelectedColor { get; set; }

        [MaxLength(20)]
        public string? SelectedSize { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("CustomDesignId")]
        public virtual CustomDesign? CustomDesign { get; set; }
        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }
    }
}
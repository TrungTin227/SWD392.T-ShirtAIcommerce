using BusinessObjects.Cart;
using BusinessObjects.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Products
{
    public class ProductVariant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Màu sắc là bắt buộc")]
        [MaxLength(50)]
        public string Color { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kích thước là bắt buộc")]
        [MaxLength(20)]
        public string Size { get; set; } = string.Empty;

        [Required(ErrorMessage = "SKU biến thể là bắt buộc")]
        [MaxLength(100)]
        public string VariantSku { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải >= 0")]
        public int Quantity { get; set; } = 0;

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(12,2)")]
        public decimal? PriceAdjustment { get; set; } = 0; // Điều chỉnh giá so với sản phẩm gốc

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
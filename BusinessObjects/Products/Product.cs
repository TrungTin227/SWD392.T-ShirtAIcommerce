using BusinessObjects.Cart;
using BusinessObjects.Entities.AI;
using BusinessObjects.Identity;
using BusinessObjects.Orders;
using BusinessObjects.Reviews;
using BusinessObjects.Wishlists;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Products
{
    public enum ProductStatus
    {
        Active,
        Inactive,
        OutOfStock,
        Discontinued
    }

    public class Product : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tên sản phẩm không được vượt quá 255 ký tự")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, 999999.99, ErrorMessage = "Giá phải từ 0.01 đến 999,999.99")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Price { get; set; }

        [Range(0.01, 999999.99, ErrorMessage = "Giá khuyến mãi phải từ 0.01 đến 999,999.99")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal? SalePrice { get; set; }

        [MaxLength(100)]
        [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "SKU chỉ được chứa chữ hoa, số và dấu gạch ngang")]
        public string? Sku { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải >= 0")]
        public int Quantity { get; set; } = 0;

        public Guid? CategoryId { get; set; }

        [MaxLength(100)]
        public string? Material { get; set; }

        [MaxLength(50)]
        public string? Season { get; set; }

        // Thuộc tính mới cho e-commerce
        [Range(0.01, 999.99, ErrorMessage = "Trọng lượng phải từ 0.01 đến 999.99 kg")]
        [Column(TypeName = "decimal(6,2)")]
        public decimal Weight { get; set; } = 0.5m; // kg

        [MaxLength(50)]
        public string? Dimensions { get; set; } // "30x40x2 cm"

        [MaxLength(160)]
        public string? MetaTitle { get; set; }

        [MaxLength(320)]
        public string? MetaDescription { get; set; }

        [MaxLength(255)]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug chỉ được chứa chữ thường, số và dấu gạch ngang")]
        public string? Slug { get; set; }

        [Range(0, int.MaxValue)]
        public int ViewCount { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int SoldCount { get; set; } = 0;

        [Range(1, 100, ErrorMessage = "Số lượng đặt hàng tối thiểu từ 1-100")]
        public int MinOrderQuantity { get; set; } = 1;

        [Range(1, 1000, ErrorMessage = "Số lượng đặt hàng tối đa từ 1-1000")]
        public int MaxOrderQuantity { get; set; } = 100;

        public bool IsFeatured { get; set; } = false;
        public bool IsBestseller { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá từ 0-100")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; } = 0;

        // JSON arrays
        public string? AvailableColors { get; set; } // JSON: ["Red", "Blue", "White"]
        public string? AvailableSizes { get; set; } // JSON: ["S", "M", "L", "XL"]
        public string? Images { get; set; } // JSON array of image URLs

        [Required]
        public ProductStatus Status { get; set; } = ProductStatus.Active;

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? Creator { get; set; }

        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<AiRecommendation> AiRecommendations { get; set; } = new List<AiRecommendation>();
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

        // Custom validation method
        public bool IsValidSalePrice()
        {
            return SalePrice == null || SalePrice < Price;
        }
    }
}
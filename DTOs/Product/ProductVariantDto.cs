using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Products
{
    public class ProductVariantDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public ProductColor Color { get; set; }
        public ProductSize Size { get; set; } 
        public string? VariantSku { get; set; }
        public int Quantity { get; set; }
        public decimal? PriceAdjustment { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        // Product Info (navigation, if needed)
        public string? ProductName { get; set; }
    }

    public class ProductVariantCreateDto
    {
        [Required]
        public Guid ProductId { get; set; }
        public ProductColor Color { get; set; } 

        public ProductSize Size { get; set; }

        [MaxLength(100)]
        public string? VariantSku { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal? PriceAdjustment { get; set; } = 0m;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ProductVariantUpdateDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public string Color { get; set; } = string.Empty;

        [Required]
        public string Size { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? VariantSku { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal? PriceAdjustment { get; set; } = 0m;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
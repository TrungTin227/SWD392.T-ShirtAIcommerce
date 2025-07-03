using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Product
{
    public class CreateProductDto
    {
        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Range(0.01, 999999.99)]
        public decimal? SalePrice { get; set; }

        [MaxLength(100)]
        public string? Sku { get; set; }

        public Guid? CategoryId { get; set; }

        [Required]
        public ProductMaterial Material { get; set; }

        [Required]
        public ProductSeason Season { get; set; }

        [MaxLength(160)]
        public string? MetaTitle { get; set; }

        [MaxLength(320)]
        public string? MetaDescription { get; set; }

        [MaxLength(255)]
        public string? Slug { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        // New: Add Images if want upload at creation
        public List<ProductImageDto>? Images { get; set; }
    }
}
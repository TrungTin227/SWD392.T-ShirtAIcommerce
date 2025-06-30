using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Product
{
    public class UpdateProductDto
    {
        [MaxLength(255, ErrorMessage = "Tên sản phẩm không được vượt quá 255 ký tự")]
        public string? Name { get; set; }

        [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
        public string? Description { get; set; }

        [Range(0.01, 999999.99, ErrorMessage = "Giá phải từ 0.01 đến 999,999.99")]
        public decimal? Price { get; set; }

        [Range(0.01, 999999.99, ErrorMessage = "Giá khuyến mãi phải từ 0.01 đến 999,999.99")]
        public decimal? SalePrice { get; set; }

        [MaxLength(100)]
        [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "SKU chỉ được chứa chữ hoa, số và dấu gạch ngang")]
        public string? Sku { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải >= 0")]
        public int? Quantity { get; set; }

        public Guid? CategoryId { get; set; }

        public ProductMaterial? Material { get; set; }
        public ProductSeason? Season { get; set; }

        [Range(0.01, 999.99, ErrorMessage = "Trọng lượng phải từ 0.01 đến 999.99 kg")]
        public decimal? Weight { get; set; }

        [MaxLength(50)]
        public string? Dimensions { get; set; }

        [MaxLength(160)]
        public string? MetaTitle { get; set; }

        [MaxLength(320)]
        public string? MetaDescription { get; set; }

        [MaxLength(255)]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug chỉ được chứa chữ thường, số và dấu gạch ngang")]
        public string? Slug { get; set; }

        [Range(1, 100, ErrorMessage = "Số lượng đặt hàng tối thiểu từ 1-100")]
        public int? MinOrderQuantity { get; set; }

        [Range(1, 1000, ErrorMessage = "Số lượng đặt hàng tối đa từ 1-1000")]
        public int? MaxOrderQuantity { get; set; }

        public bool? IsFeatured { get; set; }
        public bool? IsBestseller { get; set; }

        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá từ 0-100")]
        public decimal? DiscountPercentage { get; set; }

        public List<ProductColor>? AvailableColors { get; set; }
        public List<ProductSize>? AvailableSizes { get; set; }
        public string? Images { get; set; }
        public ProductStatus? Status { get; set; }
    }
}
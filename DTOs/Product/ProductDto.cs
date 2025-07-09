using BusinessObjects.Common;

namespace DTOs.Product
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; } // tổng hợp từ navigation property
        public ProductMaterial Material { get; set; }
        public ProductSeason Season { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Slug { get; set; }
        public int SoldCount { get; set; }
        public List<string>? Images { get; set; } // nên chuyển sang List<string> nếu ProductImage chỉ có Url
        public ProductStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
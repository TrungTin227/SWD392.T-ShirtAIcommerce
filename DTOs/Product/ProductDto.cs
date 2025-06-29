using BusinessObjects.Products;

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
        public string? CategoryName { get; set; }
        public string? Material { get; set; }
        public string? Season { get; set; }
        public decimal Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Slug { get; set; }
        public int ViewCount { get; set; }
        public int SoldCount { get; set; }
        public int MinOrderQuantity { get; set; }
        public int MaxOrderQuantity { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsBestseller { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string? AvailableColors { get; set; }
        public string? AvailableSizes { get; set; }
        public string? Images { get; set; }
        public ProductStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
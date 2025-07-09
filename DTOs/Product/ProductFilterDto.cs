using BusinessObjects.Common;
using DTOs.Common;

namespace DTOs.Product
{
    public class ProductFilterDto : PaginationDto
    {
        public string? Name { get; set; }
        public string? Sku { get; set; }
        public Guid? CategoryId { get; set; }
        public ProductStatus? Status { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public ProductMaterial? Material { get; set; }
        public ProductSeason? Season { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
    }
}
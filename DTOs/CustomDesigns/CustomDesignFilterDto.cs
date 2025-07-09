using BusinessObjects.Common;

namespace DTOs.CustomDesigns
{
    public class CustomDesignFilterDto
    {
        public Guid? UserId { get; set; }
        public Guid? StaffId { get; set; }
        public DesignStatus? Status { get; set; }
        public GarmentType? ShirtType { get; set; }
        public ProductColor? BaseColor { get; set; }
        public TShirtSize? Size { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string OrderBy { get; set; } = "CreatedAt";
        public bool OrderByDescending { get; set; } = true;
    }
}
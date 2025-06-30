using BusinessObjects.Products;

namespace DTOs.Coupons
{
    public class CouponFilterDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public CouponType? Type { get; set; }
        public CouponStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsExpired { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }
}
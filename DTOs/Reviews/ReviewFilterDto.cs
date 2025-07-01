using BusinessObjects.Reviews;

namespace DTOs.Reviews
{
    public class ReviewFilterDto
    {
        public Guid? ProductId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? OrderId { get; set; }
        public int? Rating { get; set; }
        public ReviewStatus? Status { get; set; }
        public bool? IsVerifiedPurchase { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string OrderBy { get; set; } = "CreatedAt";
        public bool OrderByDescending { get; set; } = true;
    }
}
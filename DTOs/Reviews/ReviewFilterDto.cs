using BusinessObjects.Reviews;
using DTOs.Common; 

namespace DTOs.Reviews
{
    public class ReviewFilterDto : PagedResponse<Review>
    {
        public Guid? ProductVariantId { get; set; }
        public Guid? UserId { get; set; }
        public int? Rating { get; set; }
        public ReviewStatus? Status { get; set; }

        public string OrderBy { get; set; } = "CreatedAt";
        public bool OrderByDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

    }
}
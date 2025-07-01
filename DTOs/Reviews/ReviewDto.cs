using BusinessObjects.Reviews;

namespace DTOs.Reviews
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public Guid? OrderId { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<string>? Images { get; set; }
        public int HelpfulCount { get; set; }
        public int UnhelpfulCount { get; set; }
        public ReviewStatus Status { get; set; }
        public string? AdminNotes { get; set; }
        public bool IsVerifiedPurchase { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
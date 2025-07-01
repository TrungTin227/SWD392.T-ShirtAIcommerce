namespace DTOs.Reviews
{
    public class ReviewStatsDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public int VerifiedPurchasesCount { get; set; }
    }

    public class ProductReviewSummaryDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public ReviewStatsDto Stats { get; set; } = new();
        public List<ReviewDto> RecentReviews { get; set; } = new();
    }
}
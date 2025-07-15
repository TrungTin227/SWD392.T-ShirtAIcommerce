using System.ComponentModel.DataAnnotations;

namespace DTOs.Reviews
{
    public class CreateReviewDto
    {
        [Required(ErrorMessage = "Product Variant ID is required")]
        public Guid ProductVariantId { get; set; } 

        [Required(ErrorMessage = "Order ID is required to verify purchase")]
        public Guid OrderId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Review content is required")]
        [MaxLength(1000, ErrorMessage = "Review content cannot exceed 1000 characters")]
        public string Content { get; set; } = string.Empty;

        public List<string>? Images { get; set; }
    }
}
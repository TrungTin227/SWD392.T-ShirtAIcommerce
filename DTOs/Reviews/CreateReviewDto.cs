using System.ComponentModel.DataAnnotations;

namespace DTOs.Reviews
{
    public class CreateReviewDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public Guid ProductId { get; set; }

        public Guid? OrderId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Review content is required")]
        [MaxLength(1000, ErrorMessage = "Review content cannot exceed 1000 characters")]
        public string Content { get; set; } = string.Empty;

        public List<string>? Images { get; set; }
    }
}
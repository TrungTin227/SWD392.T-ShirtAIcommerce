using BusinessObjects.Reviews;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Reviews
{
    public class UpdateReviewDto
    {
        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Review content is required")]
        [MaxLength(1000, ErrorMessage = "Review content cannot exceed 1000 characters")]
        public string Content { get; set; } = string.Empty;

        public List<string>? Images { get; set; }
    }

    public class AdminUpdateReviewDto
    {
        [Required]
        public ReviewStatus Status { get; set; }

        [MaxLength(500, ErrorMessage = "Admin notes cannot exceed 500 characters")]
        public string? AdminNotes { get; set; }
    }
}
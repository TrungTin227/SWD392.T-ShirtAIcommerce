using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.Comparisons
{
    public class ProductComparison
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        public string ProductIds { get; set; } = string.Empty; // JSON array of product IDs

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
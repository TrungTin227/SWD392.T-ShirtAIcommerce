using BusinessObjects.Identity;
using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Entities.AI
{
    // KHÔNG kế thừa BaseEntity - dữ liệu AI, chỉ cần created_at
    public class AiRecommendation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }
        public Guid? ProductId { get; set; }

        [MaxLength(50)]
        public string? RecommendationType { get; set; } // Similar Products, Design Suggestions

        public string? RecommendationData { get; set; } // JSON data

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
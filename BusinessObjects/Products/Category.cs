using BusinessObjects.Identity;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.Products
{
    // CÓ THỂ kế thừa BaseEntity - có created_at và có thể cần tracking
    public class Category : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
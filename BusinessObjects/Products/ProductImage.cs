using BusinessObjects.Identity;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.Products
{
    public class ProductImage : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }

        [Required, MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        public virtual Product Product { get; set; } = null!;
    }
}

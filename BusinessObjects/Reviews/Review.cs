using BusinessObjects.Identity;
using BusinessObjects.Orders;
using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Reviews
{
    public enum ReviewStatus
    {
        Approved,
        Pending,
        Hidden,
        Rejected
    }

    public class Review : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "ID người dùng là bắt buộc")]
        public Guid UserId { get; set; }

        public Guid? ProductId { get; set; }
        public Guid? OrderId { get; set; }

        [Required(ErrorMessage = "Đánh giá là bắt buộc")]
        [Range(1, 5, ErrorMessage = "Đánh giá từ 1-5 sao")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Nội dung đánh giá là bắt buộc")]
        [MaxLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự")]
        public string Content { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Images { get; set; } // JSON array of review images

        [Range(0, int.MaxValue)]
        public int HelpfulCount { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int UnhelpfulCount { get; set; } = 0;

        [Required]
        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

        [MaxLength(500)]
        public string? AdminNotes { get; set; }

        public bool IsVerifiedPurchase { get; set; } = false;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}
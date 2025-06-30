using BusinessObjects.Cart;
using BusinessObjects.Identity;
using BusinessObjects.Orders;
using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.CustomDesigns
{
    public class CustomDesign : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "ID người dùng là bắt buộc")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Tên thiết kế là bắt buộc")]
        [MaxLength(255)]
        public string DesignName { get; set; } = string.Empty;

        [Required]
        public GarmentType ShirtType { get; set; }
        [Required]
        public ProductColor BaseColor { get; set; }

        [Required(ErrorMessage = "Kích thước là bắt buộc")]
        public TShirtSize Size { get; set; }

        [Url(ErrorMessage = "URL hình ảnh không hợp lệ")]
        [MaxLength(500)]
        public string? DesignImageUrl { get; set; }

        [MaxLength(255)]
        public string? LogoText { get; set; }

        public LogoPosition? LogoPosition { get; set; }

        [MaxLength(1000)]
        public string? SpecialRequirements { get; set; }

        [Required(ErrorMessage = "Tổng giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tổng giá phải lớn hơn 0")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalPrice { get; set; }

        [Range(1, 100, ErrorMessage = "Số lượng từ 1-100")]
        public int Quantity { get; set; } = 1;

        [Range(1, 30, ErrorMessage = "Thời gian hoàn thành từ 1-30 ngày")]
        public int EstimatedDays { get; set; } = 7;

        [Required]
        public DesignStatus Status { get; set; } = DesignStatus.Draft;

        public Guid? StaffId { get; set; }

        [MaxLength(1000)]
        public string? StaffNotes { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("StaffId")]
        public virtual ApplicationUser? Staff { get; set; }

        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
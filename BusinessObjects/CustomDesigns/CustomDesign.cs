using BusinessObjects.Cart;
using BusinessObjects.Common;
using BusinessObjects.Identity;
using BusinessObjects.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.CustomDesigns
{
    public class CustomDesign : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(255)]
        public string DesignName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? PromptText { get; set; }

        [Required]
        public GarmentType ShirtType { get; set; }

        [Required]
        public ProductColor BaseColor { get; set; }

        [Required]
        public TShirtSize Size { get; set; }

        [MaxLength(500)]
        public string? DesignImageUrl { get; set; }

        [MaxLength(1000)]
        public string? SpecialRequirements { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalPrice { get; set; }

        public int Quantity { get; set; } = 1;

        [Required]
        public CustomDesignStatus Status { get; set; } = CustomDesignStatus.Draft;

        // Liên kết ngược tới user (optional, có thể xóa nếu không dùng)
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
        //Thờigian tạo và cập nhật
        public DateTime? OrderCreatedAt { get; set; }
        public DateTime? ShippingStartedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? DoneAt { get; set; }

        // Các navigation tới CartItem, OrderItem (optional, có thể xóa nếu không dùng)
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
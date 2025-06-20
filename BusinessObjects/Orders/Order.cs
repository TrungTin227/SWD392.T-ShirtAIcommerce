using BusinessObjects.Coupons;
using BusinessObjects.Entities.Payments;
using BusinessObjects.Identity;
using BusinessObjects.Reviews;
using BusinessObjects.Shipping;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Orders
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Processing,
        Shipping,
        Delivered,
        Cancelled,
        Returned
    }

    public enum PaymentStatus
    {
        Unpaid,
        Paid,
        PartiallyPaid,
        Refunded,
        PartiallyRefunded
    }

    public class Order : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Mã đơn hàng là bắt buộc")]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID người dùng là bắt buộc")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Tổng tiền là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn 0")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Phí vận chuyển phải >= 0")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm giá phải >= 0")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Thuế phải >= 0")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền hoàn lại phải >= 0")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal RefundAmount { get; set; } = 0;

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [MaxLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại người nhận là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20)]
        public string ReceiverPhone { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? CustomerNotes { get; set; }

        public DateTime? EstimatedDeliveryDate { get; set; }

        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public Guid? AssignedStaffId { get; set; }

        public Guid? CouponId { get; set; }

        public Guid? ShippingMethodId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("AssignedStaffId")]
        public virtual ApplicationUser? AssignedStaff { get; set; }

        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }

        [ForeignKey("ShippingMethodId")]
        public virtual ShippingMethod? ShippingMethod { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
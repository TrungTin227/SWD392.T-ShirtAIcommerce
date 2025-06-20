using BusinessObjects.Identity;
using BusinessObjects.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Coupons
{
    public enum CouponType
    {
        Percentage,
        FixedAmount,
        FreeShipping
    }

    public enum CouponStatus
    {
        Active,
        Inactive,
        Expired,
        Used
    }

    public class Coupon : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Mã coupon là bắt buộc")]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên coupon là bắt buộc")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public CouponType Type { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Value { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(12,2)")]
        public decimal? MinOrderAmount { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(12,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        [Range(1, int.MaxValue)]
        public int? UsageLimit { get; set; }

        [Range(0, int.MaxValue)]
        public int UsedCount { get; set; } = 0;

        [Range(1, int.MaxValue)]
        public int? UsageLimitPerUser { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public CouponStatus Status { get; set; } = CouponStatus.Active;

        public bool IsFirstTimeUserOnly { get; set; } = false;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
    }
}
using BusinessObjects.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Coupons
{
    public class UserCoupon
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid CouponId { get; set; }

        [Range(0, int.MaxValue)]
        public int UsedCount { get; set; } = 0;

        public DateTime FirstUsedAt { get; set; }
        public DateTime LastUsedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("CouponId")]
        public virtual Coupon Coupon { get; set; } = null!;
    }
}
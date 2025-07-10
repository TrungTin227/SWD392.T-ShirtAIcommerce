using BusinessObjects.Common;
using DTOs.Coupons;

namespace DTOs.UserCoupons
{
    public class UserCouponDto
    {
        public Guid Id { get; set; }
        public Guid CouponId { get; set; }
        public Guid UserId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime ClaimedAt { get; set; }
        public DateTime? ValidUntil { get; set; }
        public CouponStatus? Status { get; set; }
        public CouponDto Coupon { get; set; } = null!;

    }
}
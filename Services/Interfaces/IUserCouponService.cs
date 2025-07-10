using DTOs.UserCoupons;
using Repositories.Commons;

namespace Services.Interfaces
{
    public interface IUserCouponService
    {
        /// <summary>
        /// Claim a coupon for current user.
        /// </summary>
        Task<ApiResult<UserCouponDto>> ClaimAsync(Guid couponId);

        /// <summary>
        /// Get all valid claimed coupons for current user.
        /// </summary>
        Task<ApiResult<IEnumerable<UserCouponDto>>> GetClaimedCouponsAsync();

        /// <summary>
        /// Unclaim (delete) one or more claimed coupons by their UserCoupon Ids.
        /// </summary>
        Task<ApiResult<bool>> UnclaimAsync(IEnumerable<Guid> userCouponIds);
    }
}

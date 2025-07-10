using BusinessObjects.Coupons;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IUserCouponRepository : IGenericRepository<UserCoupon, Guid>
    {
        /// <summary>
        /// Get all valid claimed coupons for a user, considering UsageLimit and UsageLimitPerUser.
        /// </summary>
        Task<IEnumerable<UserCoupon>> GetValidClaimedCouponsAsync(Guid userId);
    }
}

using BusinessObjects.Common;
using BusinessObjects.Coupons;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class UserCouponRepository : GenericRepository<UserCoupon, Guid>, IUserCouponRepository
    {
        private readonly T_ShirtAIcommerceContext _context;
        private readonly ICurrentTime _currentTime;

        public UserCouponRepository(T_ShirtAIcommerceContext context, ICurrentTime currentTime) : base(context)
        {
            _context = context;
            _currentTime=currentTime;
        }

        public async Task<IEnumerable<UserCoupon>> GetValidClaimedCouponsAsync(Guid userId)
        {
            // 1. Lấy thời điểm hiện tại (VN) đúng kiểu DateTime
            var now = _currentTime.GetVietnamTime();

            // 2. Truy vấn UserCoupon kèm Coupon
            var userCoupons = await _context.Set<UserCoupon>()
                .Include(uc => uc.Coupon)
                .Where(uc =>
                    uc.UserId == userId &&

                    // Loại bỏ coupon đã xóa
                    !uc.Coupon.IsDeleted &&

                    // Chỉ lấy coupon đang Active
                    uc.Coupon.Status == CouponStatus.Active &&

                    // Chỉ lấy coupon trong khoảng date hợp lệ
                    uc.Coupon.StartDate <= now &&
                    uc.Coupon.EndDate   >= now &&

                    // Kiểm tra global usage limit
                    (!uc.Coupon.UsageLimit.HasValue || uc.Coupon.UsedCount < uc.Coupon.UsageLimit.Value) &&

                    // Kiểm tra per-user usage limit
                    (!uc.Coupon.UsageLimitPerUser.HasValue || uc.UsedCount < uc.Coupon.UsageLimitPerUser.Value)
                )
                .ToListAsync();

            return userCoupons;
        }

    }
}
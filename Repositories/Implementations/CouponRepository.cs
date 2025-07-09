using BusinessObjects.Common;
using BusinessObjects.Coupons;
using DTOs.Coupons;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implementations
{
    public class CouponRepository : GenericRepository<Coupon, Guid>, ICouponRepository
    {
        public CouponRepository(T_ShirtAIcommerceContext context) : base(context)
        {
        }

        public async Task<PagedList<Coupon>> GetCouponsAsync(CouponFilterDto filter)
        {
            var query = _dbSet.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Code))
                query = query.Where(c => c.Code.Contains(filter.Code));

            if (!string.IsNullOrEmpty(filter.Name))
                query = query.Where(c => c.Name.Contains(filter.Name));

            if (filter.Type.HasValue)
                query = query.Where(c => c.Type == filter.Type.Value);

            if (filter.Status.HasValue)
                query = query.Where(c => c.Status == filter.Status.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(c => c.StartDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(c => c.EndDate <= filter.EndDate.Value);

            if (filter.MinValue.HasValue)
                query = query.Where(c => c.Value >= filter.MinValue.Value);

            if (filter.MaxValue.HasValue)
                query = query.Where(c => c.Value <= filter.MaxValue.Value);

            if (filter.IsActive.HasValue)
            {
                var now = DateTime.UtcNow;
                if (filter.IsActive.Value)
                    query = query.Where(c => c.Status == CouponStatus.Active && c.StartDate <= now && c.EndDate >= now);
                else
                    query = query.Where(c => c.Status != CouponStatus.Active || c.StartDate > now || c.EndDate < now);
            }

            if (filter.IsExpired.HasValue)
            {
                var now = DateTime.UtcNow;
                if (filter.IsExpired.Value)
                    query = query.Where(c => c.EndDate < now);
                else
                    query = query.Where(c => c.EndDate >= now);
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "code" => filter.SortDescending
                    ? query.OrderByDescending(c => c.Code)
                    : query.OrderBy(c => c.Code),
                "name" => filter.SortDescending
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name),
                "value" => filter.SortDescending
                    ? query.OrderByDescending(c => c.Value)
                    : query.OrderBy(c => c.Value),
                "status" => filter.SortDescending
                    ? query.OrderByDescending(c => c.Status)
                    : query.OrderBy(c => c.Status),
                "startdate" => filter.SortDescending
                    ? query.OrderByDescending(c => c.StartDate)
                    : query.OrderBy(c => c.StartDate),
                "enddate" => filter.SortDescending
                    ? query.OrderByDescending(c => c.EndDate)
                    : query.OrderBy(c => c.EndDate),
                _ => filter.SortDescending
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize)
                                  .Take(filter.PageSize)
                                  .ToListAsync();

            return new PagedList<Coupon>(items, totalCount, filter.PageNumber, filter.PageSize);
        }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            return await _dbSet
                .Include(c => c.UserCoupons)
                .FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);
        }

        public async Task<IEnumerable<Coupon>> GetActiveCouponsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(c => c.Status == CouponStatus.Active &&
                           c.StartDate <= now &&
                           c.EndDate >= now &&
                           !c.IsDeleted)
                .OrderBy(c => c.EndDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Coupon>> GetExpiredCouponsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(c => c.EndDate < now && !c.IsDeleted)
                .OrderByDescending(c => c.EndDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Coupon>> GetUserAvailableCouponsAsync(Guid userId)
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(c => c.Status == CouponStatus.Active &&
                           c.StartDate <= now &&
                           c.EndDate >= now &&
                           !c.IsDeleted &&
                           (!c.UsageLimit.HasValue || c.UsedCount < c.UsageLimit.Value))
                .Where(c => !c.UsageLimitPerUser.HasValue ||
                           !c.UserCoupons.Any(uc => uc.UserId == userId &&
                                                   uc.UsedCount >= c.UsageLimitPerUser.Value))
                .OrderBy(c => c.EndDate)
                .ToListAsync();
        }

        public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null)
        {
            var query = _dbSet.Where(c => c.Code == code && !c.IsDeleted);

            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);

            return !await query.AnyAsync();
        }

        public async Task<bool> CanUserUseCouponAsync(Guid couponId, Guid userId)
        {
            var coupon = await _dbSet
                .Include(c => c.UserCoupons)
                .FirstOrDefaultAsync(c => c.Id == couponId && !c.IsDeleted);

            if (coupon == null) return false;

            var now = DateTime.UtcNow;

            // Check basic coupon validity
            if (coupon.Status != CouponStatus.Active ||
                coupon.StartDate > now ||
                coupon.EndDate < now)
                return false;

            // Check global usage limit
            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                return false;

            // Check per-user usage limit
            if (coupon.UsageLimitPerUser.HasValue)
            {
                var userUsage = coupon.UserCoupons
                    .Where(uc => uc.UserId == userId)
                    .Sum(uc => uc.UsedCount);

                if (userUsage >= coupon.UsageLimitPerUser.Value)
                    return false;
            }

            return true;
        }

        public async Task<int> GetUserCouponUsageCountAsync(Guid couponId, Guid userId)
        {
            return await _context.Set<UserCoupon>()
                .Where(uc => uc.CouponId == couponId && uc.UserId == userId)
                .SumAsync(uc => uc.UsedCount);
        }

        public async Task<bool> IncrementUsageCountAsync(Guid couponId)
        {
            var coupon = await _dbSet.FindAsync(couponId);
            if (coupon == null) return false;

            coupon.UsedCount++;
            coupon.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public async Task<bool> DecrementUsageCountAsync(Guid couponId)
        {
            var coupon = await _dbSet.FindAsync(couponId);
            if (coupon == null || coupon.UsedCount <= 0) return false;

            coupon.UsedCount--;
            coupon.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public async Task<IEnumerable<Coupon>> GetCouponsByTypeAsync(CouponType type)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(c => c.Type == type &&
                           c.Status == CouponStatus.Active &&
                           c.StartDate <= now &&
                           c.EndDate >= now &&
                           !c.IsDeleted)
                .OrderBy(c => c.Value)
                .ToListAsync();
        }

        public async Task<decimal> CalculateDiscountAmountAsync(Guid couponId, decimal orderAmount)
        {
            var coupon = await _dbSet.FindAsync(couponId);
            if (coupon == null) return 0;

            return coupon.Type switch
            {
                CouponType.Percentage => Math.Min(
                    orderAmount * coupon.Value / 100,
                    coupon.MaxDiscountAmount ?? decimal.MaxValue
                ),
                CouponType.FixedAmount => Math.Min(coupon.Value, orderAmount),
                CouponType.FreeShipping => 0, // Handle in shipping calculation
                _ => 0
            };
        }

        public async Task<bool> ValidateCouponForOrderAsync(Guid couponId, decimal orderAmount, Guid? userId = null)
        {
            var coupon = await _dbSet
                .Include(c => c.UserCoupons)
                .FirstOrDefaultAsync(c => c.Id == couponId && !c.IsDeleted);

            if (coupon == null) return false;

            var now = DateTime.UtcNow;

            // Check basic validity
            if (coupon.Status != CouponStatus.Active ||
                coupon.StartDate > now ||
                coupon.EndDate < now)
                return false;

            // Check minimum order amount
            if (coupon.MinOrderAmount.HasValue && orderAmount < coupon.MinOrderAmount.Value)
                return false;

            // Check global usage limit
            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                return false;

            // Check user-specific constraints if userId provided
            if (userId.HasValue && !await CanUserUseCouponAsync(couponId, userId.Value))
                return false;

            return true;
        }
    }
}
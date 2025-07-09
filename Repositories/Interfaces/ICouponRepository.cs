using BusinessObjects.Common;
using BusinessObjects.Coupons;
using DTOs.Coupons;
using Repositories.Helpers;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface ICouponRepository : IGenericRepository<Coupon, Guid>
    {
        Task<PagedList<Coupon>> GetCouponsAsync(CouponFilterDto filter);
        Task<Coupon?> GetByCodeAsync(string code);
        Task<IEnumerable<Coupon>> GetActiveCouponsAsync();
        Task<IEnumerable<Coupon>> GetExpiredCouponsAsync();
        Task<IEnumerable<Coupon>> GetUserAvailableCouponsAsync(Guid userId);
        Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null);
        Task<bool> CanUserUseCouponAsync(Guid couponId, Guid userId);
        Task<int> GetUserCouponUsageCountAsync(Guid couponId, Guid userId);
        Task<bool> IncrementUsageCountAsync(Guid couponId);
        Task<bool> DecrementUsageCountAsync(Guid couponId);
        Task<IEnumerable<Coupon>> GetCouponsByTypeAsync(CouponType type);
        Task<decimal> CalculateDiscountAmountAsync(Guid couponId, decimal orderAmount);
        Task<bool> ValidateCouponForOrderAsync(Guid couponId, decimal orderAmount, Guid? userId = null);
    }
}
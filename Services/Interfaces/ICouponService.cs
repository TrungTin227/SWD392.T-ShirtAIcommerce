using DTOs.Coupons;
using Repositories.Commons;
using Repositories.Helpers;

namespace Services.Interfaces
{
    public interface ICouponService
    {
        Task<ApiResult<CouponDto>> GetByIdAsync(Guid id);
        Task<ApiResult<CouponDto>> GetByCodeAsync(string code);
        Task<ApiResult<PagedList<CouponDto>>> GetCouponsAsync(CouponFilterDto filter);
        Task<ApiResult<CouponDto>> CreateCouponAsync(CreateCouponDto createDto);
        Task<ApiResult<CouponDto>> UpdateCouponAsync(Guid id, UpdateCouponDto updateDto);
        Task<ApiResult<bool>> DeleteCouponAsync(Guid id);
        Task<ApiResult<IEnumerable<CouponDto>>> GetActiveCouponsAsync();
        Task<ApiResult<IEnumerable<CouponDto>>> GetUserAvailableCouponsAsync(Guid userId);
        Task<ApiResult<CouponDiscountResultDto>> ApplyCouponAsync(ApplyCouponDto applyDto);
        Task<ApiResult<bool>> ValidateCouponAsync(string code, decimal orderAmount, Guid? userId = null);
        Task<ApiResult<decimal>> CalculateDiscountAsync(string code, decimal orderAmount);
        Task<ApiResult<bool>> UseCouponAsync(Guid couponId, Guid userId);
    }
}
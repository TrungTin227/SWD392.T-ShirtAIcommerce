using BusinessObjects.Common;
using BusinessObjects.Coupons;
using DTOs.UserCoupons;
using Microsoft.EntityFrameworkCore;
using Repositories.Commons;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Interfaces;

namespace Services.Implementations
{
    public class UserCouponService : BaseService<UserCoupon, Guid>, IUserCouponService
    {
        private readonly ICouponRepository _couponRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICurrentTime _currentTime;

        public UserCouponService(
            IGenericRepository<UserCoupon, Guid> genericRepo,
            ICouponRepository couponRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(genericRepo, currentUserService, unitOfWork, currentTime)
        {
            _couponRepository = couponRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _currentTime = currentTime;
        }

        public async Task<ApiResult<UserCouponDto>> ClaimAsync(Guid couponId)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var userId = _currentUserService.GetUserId();
                if (userId == Guid.Empty) return ApiResult<UserCouponDto>.Failure("Người dùng không xác thực");

                // 1. Kiểm tra coupon hợp lệ và user có thể claim không
                var canUse = await _couponRepository.CanUserUseCouponAsync(couponId, userId.Value);
                if (!canUse) return ApiResult<UserCouponDto>.Failure("Coupon không hợp lệ hoặc đã hết lượt");

                // 2. Kiểm tra user đã claim chưa
                var db = _unitOfWork.Context;
                var existed = await db.Set<UserCoupon>().FirstOrDefaultAsync(
                    uc => uc.CouponId == couponId && uc.UserId == userId);

                if (existed != null)
                    return ApiResult<UserCouponDto>.Failure("Bạn đã claim coupon này rồi");

                // 3. Tạo bản ghi claim mới
                var now = _currentTime.GetVietnamTime();
                var userCoupon = new UserCoupon
                {
                    Id = Guid.NewGuid(),
                    CouponId = couponId,
                    UserId = userId.Value,
                    UsedCount = 0,
                    FirstUsedAt = now,
                    LastUsedAt = now
                };
                await db.Set<UserCoupon>().AddAsync(userCoupon);

                // 4. Trả về kết quả
                var coupon = await _couponRepository.GetByIdAsync(couponId);
                var dto = new UserCouponDto
                {
                    Id = userCoupon.Id,
                    CouponId = couponId,
                    UserId = userId.Value,
                    Code = coupon?.Code ?? "",
                    Name = coupon?.Name ?? "",
                    ClaimedAt = now,
                    ValidUntil = coupon?.EndDate,
                    Status = coupon?.Status
                };
                return ApiResult<UserCouponDto>.Success(dto, "Claim thành công");
            });
        }
        public async Task<ApiResult<IEnumerable<UserCouponDto>>> GetClaimedCouponsAsync()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                if (userId == Guid.Empty) return ApiResult<IEnumerable<UserCouponDto>>.Failure("Người dùng không xác thực");

                var now = _currentTime.GetVietnamTime();
                var db = _unitOfWork.Context;

                var claimed = await db.Set<UserCoupon>()
                    .Include(uc => uc.Coupon)
                    .Where(uc =>
                        uc.UserId == userId &&
                        !uc.Coupon.IsDeleted &&
                        uc.Coupon.Status == CouponStatus.Active &&
                        uc.Coupon.StartDate <= now &&
                        uc.Coupon.EndDate >= now &&
                        (!uc.Coupon.UsageLimitPerUser.HasValue || uc.UsedCount < uc.Coupon.UsageLimitPerUser.Value)
                    )
                    .ToListAsync();

                var dtos = claimed.Select(uc => new UserCouponDto
                {
                    Id = uc.Id,
                    CouponId = uc.CouponId,
                    UserId = uc.UserId,
                    Code = uc.Coupon.Code,
                    Name = uc.Coupon.Name,
                    ClaimedAt = uc.FirstUsedAt,
                    ValidUntil = uc.Coupon.EndDate,
                    Status = uc.Coupon.Status
                });

                return ApiResult<IEnumerable<UserCouponDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return ApiResult<IEnumerable<UserCouponDto>>.Failure("Lỗi khi lấy danh sách coupon đã claim", ex);
            }
        }

        public async Task<ApiResult<bool>> UnclaimAsync(IEnumerable<Guid> userCouponIds)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    var userId = _currentUserService.GetUserId();
                    if (userId == Guid.Empty) return ApiResult<bool>.Failure("Người dùng không xác thực");

                    var db = _unitOfWork.Context;
                    var userCoupons = await db.Set<UserCoupon>()
                        .Where(uc => userCouponIds.Contains(uc.Id) && uc.UserId == userId)
                        .ToListAsync();

                    if (!userCoupons.Any())
                        return ApiResult<bool>.Failure("Không tìm thấy coupon nào để hủy");

                    db.Set<UserCoupon>().RemoveRange(userCoupons);
                    return ApiResult<bool>.Success(true, $"Đã hủy {userCoupons.Count} coupon thành công");
                }
                catch (Exception ex)
                {
                    return ApiResult<bool>.Failure("Lỗi khi hủy claim coupon", ex);
                }
            });
        }
    }
}
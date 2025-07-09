using BusinessObjects.Common;
using BusinessObjects.Coupons;
using DTOs.Coupons;
using Microsoft.EntityFrameworkCore;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Helpers.Mappers;
using Services.Interfaces;

namespace Services.Implementations
{
    public class CouponService : BaseService<Coupon, Guid>, ICouponService
    {
        private readonly ICouponRepository _couponRepository;

        public CouponService(
            ICouponRepository couponRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(couponRepository, currentUserService, unitOfWork, currentTime)
        {
            _couponRepository = couponRepository;
        }

        public async Task<ApiResult<CouponDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var coupon = await _couponRepository.GetByIdAsync(id);
                if (coupon == null)
                    return ApiResult<CouponDto>.Failure("Không tìm thấy coupon");

                var couponDto = CouponMapper.ToDto(coupon);
                return ApiResult<CouponDto>.Success(couponDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CouponDto>.Failure("Lỗi khi lấy thông tin coupon", ex);
            }
        }

        public async Task<ApiResult<CouponDto>> GetByCodeAsync(string code)
        {
            try
            {
                var coupon = await _couponRepository.GetByCodeAsync(code);
                if (coupon == null)
                    return ApiResult<CouponDto>.Failure("Không tìm thấy coupon với mã này");

                var couponDto = CouponMapper.ToDto(coupon);
                return ApiResult<CouponDto>.Success(couponDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CouponDto>.Failure("Lỗi khi lấy thông tin coupon", ex);
            }
        }

        public async Task<ApiResult<PagedList<CouponDto>>> GetCouponsAsync(CouponFilterDto filter)
        {
            try
            {
                var pagedCoupons = await _couponRepository.GetCouponsAsync(filter);
                var couponDtos = CouponMapper.ToPagedDto(pagedCoupons);
                return ApiResult<PagedList<CouponDto>>.Success(couponDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<CouponDto>>.Failure("Lỗi khi lấy danh sách coupon", ex);
            }
        }

        public async Task<ApiResult<CouponDto>> CreateCouponAsync(CreateCouponDto createDto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    // Validate business rules
                    var validationResult = await ValidateCreateCouponAsync(createDto);
                    if (!validationResult.IsSuccess)
                        return validationResult;

                    // Check code uniqueness
                    var isCodeUnique = await _couponRepository.IsCodeUniqueAsync(createDto.Code);
                    if (!isCodeUnique)
                        return ApiResult<CouponDto>.Failure("Mã coupon đã tồn tại");

                    // Create coupon entity
                    var coupon = CouponMapper.ToEntity(createDto);
                    coupon.Status = CouponStatus.Active;

                    // Use BaseService to handle audit fields
                    var createdCoupon = await CreateAsync(coupon);
                    var couponDto = CouponMapper.ToDto(createdCoupon);

                    return ApiResult<CouponDto>.Success(couponDto, "Tạo coupon thành công");
                }
                catch (Exception ex)
                {
                    return ApiResult<CouponDto>.Failure("Lỗi khi tạo coupon", ex);
                }
            });
        }

        public async Task<ApiResult<CouponDto>> UpdateCouponAsync(Guid id, UpdateCouponDto updateDto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    var existingCoupon = await _couponRepository.GetByIdAsync(id);
                    if (existingCoupon == null)
                        return ApiResult<CouponDto>.Failure("Không tìm thấy coupon");

                    // Validate business rules
                    var validationResult = await ValidateUpdateCouponAsync(updateDto, existingCoupon);
                    if (!validationResult.IsSuccess)
                        return validationResult;

                    // Update coupon properties
                    CouponMapper.UpdateEntity(existingCoupon, updateDto);

                    // Use BaseService to handle audit fields
                    var updatedCoupon = await UpdateAsync(existingCoupon);
                    var couponDto = CouponMapper.ToDto(updatedCoupon);

                    return ApiResult<CouponDto>.Success(couponDto, "Cập nhật coupon thành công");
                }
                catch (Exception ex)
                {
                    return ApiResult<CouponDto>.Failure("Lỗi khi cập nhật coupon", ex);
                }
            });
        }

        public async Task<ApiResult<bool>> DeleteCouponAsync(Guid id)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    var coupon = await _couponRepository.GetByIdAsync(id);
                    if (coupon == null)
                        return ApiResult<bool>.Failure("Không tìm thấy coupon");

                    // Check if coupon is being used
                    if (coupon.UsedCount > 0)
                        return ApiResult<bool>.Failure("Không thể xóa coupon đã được sử dụng");

                    // Use BaseService soft delete
                    var result = await DeleteAsync(id);
                    return ApiResult<bool>.Success(result, "Xóa coupon thành công");
                }
                catch (Exception ex)
                {
                    return ApiResult<bool>.Failure("Lỗi khi xóa coupon", ex);
                }
            });
        }

        public async Task<ApiResult<IEnumerable<CouponDto>>> GetActiveCouponsAsync()
        {
            try
            {
                var coupons = await _couponRepository.GetActiveCouponsAsync();
                var couponDtos = coupons.Select(CouponMapper.ToDto);
                return ApiResult<IEnumerable<CouponDto>>.Success(couponDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<IEnumerable<CouponDto>>.Failure("Lỗi khi lấy danh sách coupon hoạt động", ex);
            }
        }

        public async Task<ApiResult<IEnumerable<CouponDto>>> GetUserAvailableCouponsAsync(Guid userId)
        {
            try
            {
                var coupons = await _couponRepository.GetUserAvailableCouponsAsync(userId);
                var couponDtos = coupons.Select(CouponMapper.ToDto);
                return ApiResult<IEnumerable<CouponDto>>.Success(couponDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<IEnumerable<CouponDto>>.Failure("Lỗi khi lấy danh sách coupon khả dụng", ex);
            }
        }

        public async Task<ApiResult<CouponDiscountResultDto>> ApplyCouponAsync(ApplyCouponDto applyDto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                // 1. Lấy coupon theo mã và kiểm tra tồn tại
                var coupon = await _couponRepository.GetByCodeAsync(applyDto.Code);
                if (coupon == null)
                {
                    return ApiResult<CouponDiscountResultDto>.Success(new CouponDiscountResultDto
                    {
                        IsValid = false,
                        Message = "Mã coupon không tồn tại"
                    });
                }

                // 2. Validate coupon với đơn hàng và user (nếu có)
                var isValid = await _couponRepository.ValidateCouponForOrderAsync(
                    coupon.Id,
                    applyDto.OrderAmount,
                    applyDto.UserId);
                if (!isValid)
                {
                    return ApiResult<CouponDiscountResultDto>.Success(new CouponDiscountResultDto
                    {
                        IsValid = false,
                        Message = "Coupon không hợp lệ hoặc không thể sử dụng"
                    });
                }

                // 3. Tăng lượt dùng toàn cục
                var incrementOk = await _couponRepository.IncrementUsageCountAsync(coupon.Id);
                if (!incrementOk)
                {
                    return ApiResult<CouponDiscountResultDto>.Failure("Không thể cập nhật lượt dùng coupon");
                }

                // 4. Ghi nhận lượt dùng cho user
                var db = _unitOfWork.Context;
                var userCoupon = await db.Set<UserCoupon>()
                    .FirstOrDefaultAsync(uc => uc.CouponId == coupon.Id && uc.UserId == applyDto.UserId);

                if (userCoupon == null)
                {
                    userCoupon = new UserCoupon
                    {
                        Id            = Guid.NewGuid(),
                        CouponId      = coupon.Id,
                        UserId        = applyDto.UserId.Value,
                        UsedCount     = 1,
                        FirstUsedAt   = _currentTime.GetVietnamTime(),
                        LastUsedAt    = _currentTime.GetVietnamTime(),
                    };
                    await db.Set<UserCoupon>().AddAsync(userCoupon);
                }
                else
                {
                    userCoupon.UsedCount++;
                    userCoupon.LastUsedAt = _currentTime.GetVietnamTime();
                }

                // 5. Tính toán số tiền giảm
                var discountAmount = await _couponRepository.CalculateDiscountAmountAsync(
                    coupon.Id,
                    applyDto.OrderAmount);

                var resultDto = new CouponDiscountResultDto
                {
                    IsValid        = true,
                    Message        = "Áp dụng coupon thành công",
                    DiscountAmount = discountAmount,
                    FinalAmount    = applyDto.OrderAmount - discountAmount,
                    Coupon         = CouponMapper.ToDto(coupon)
                };

                return ApiResult<CouponDiscountResultDto>.Success(resultDto);
            });
        }

        public async Task<ApiResult<bool>> ValidateCouponAsync(string code, decimal orderAmount, Guid? userId = null)
        {
            try
            {
                var coupon = await _couponRepository.GetByCodeAsync(code);
                if (coupon == null)
                    return ApiResult<bool>.Failure("Mã coupon không tồn tại");

                var isValid = await _couponRepository.ValidateCouponForOrderAsync(
                    coupon.Id, orderAmount, userId);

                return ApiResult<bool>.Success(isValid,
                    isValid ? "Coupon hợp lệ" : "Coupon không hợp lệ");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure("Lỗi khi validate coupon", ex);
            }
        }

        public async Task<ApiResult<decimal>> CalculateDiscountAsync(string code, decimal orderAmount)
        {
            try
            {
                var coupon = await _couponRepository.GetByCodeAsync(code);
                if (coupon == null)
                    return ApiResult<decimal>.Failure("Mã coupon không tồn tại");

                var discountAmount = await _couponRepository.CalculateDiscountAmountAsync(
                    coupon.Id, orderAmount);

                return ApiResult<decimal>.Success(discountAmount);
            }
            catch (Exception ex)
            {
                return ApiResult<decimal>.Failure("Lỗi khi tính toán giảm giá", ex);
            }
        }

        public async Task<ApiResult<bool>> UseCouponAsync(Guid couponId, Guid userId)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    // Increment global usage count
                    var incrementResult = await _couponRepository.IncrementUsageCountAsync(couponId);
                    if (!incrementResult)
                        return ApiResult<bool>.Failure("Không thể sử dụng coupon");

                    // Update or create user coupon usage
                    var userCoupon = await _unitOfWork.Context.Set<UserCoupon>()
                .FirstOrDefaultAsync(uc => uc.CouponId == couponId && uc.UserId == userId);


                    if (userCoupon == null)
                    {
                        userCoupon = new UserCoupon
                        {
                            Id = Guid.NewGuid(),
                            CouponId = couponId,
                            UserId = userId,
                            UsedCount = 1,
                            FirstUsedAt = DateTime.UtcNow,
                            LastUsedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Context.Set<UserCoupon>().AddAsync(userCoupon);
                    }
                    else
                    {
                        userCoupon.UsedCount++;
                        userCoupon.LastUsedAt = DateTime.UtcNow;
                    }

                    return ApiResult<bool>.Success(true, "Sử dụng coupon thành công");
                }
                catch (Exception ex)
                {
                    return ApiResult<bool>.Failure("Lỗi khi sử dụng coupon", ex);
                }
            });
        }

        // Private validation methods
        private async Task<ApiResult<CouponDto>> ValidateCreateCouponAsync(CreateCouponDto createDto)
        {
            if (createDto.EndDate <= createDto.StartDate)
                return ApiResult<CouponDto>.Failure("Ngày kết thúc phải sau ngày bắt đầu");

            if (createDto.StartDate < DateTime.UtcNow.Date)
                return ApiResult<CouponDto>.Failure("Ngày bắt đầu không được là quá khứ");

            if (createDto.Type == CouponType.Percentage && createDto.Value > 100)
                return ApiResult<CouponDto>.Failure("Phần trăm giảm giá không được vượt quá 100%");

            if (createDto.MaxDiscountAmount.HasValue && createDto.MaxDiscountAmount <= 0)
                return ApiResult<CouponDto>.Failure("Số tiền giảm tối đa phải lớn hơn 0");

            return ApiResult<CouponDto>.Success(null!, "Validation passed");
        }

        private async Task<ApiResult<CouponDto>> ValidateUpdateCouponAsync(UpdateCouponDto updateDto, Coupon existingCoupon)
        {
            if (updateDto.EndDate <= updateDto.StartDate)
                return ApiResult<CouponDto>.Failure("Ngày kết thúc phải sau ngày bắt đầu");

            if (updateDto.Type == CouponType.Percentage && updateDto.Value > 100)
                return ApiResult<CouponDto>.Failure("Phần trăm giảm giá không được vượt quá 100%");

            if (updateDto.MaxDiscountAmount.HasValue && updateDto.MaxDiscountAmount <= 0)
                return ApiResult<CouponDto>.Failure("Số tiền giảm tối đa phải lớn hơn 0");

            // Cannot reduce usage limit below current usage
            if (updateDto.UsageLimit.HasValue && updateDto.UsageLimit.Value < existingCoupon.UsedCount)
                return ApiResult<CouponDto>.Failure("Không thể giảm giới hạn sử dụng xuống dưới số lần đã sử dụng");

            return ApiResult<CouponDto>.Success(null!, "Validation passed");
        }
    }
}
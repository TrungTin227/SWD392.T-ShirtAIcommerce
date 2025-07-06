using BusinessObjects.Coupons;
using BusinessObjects.Products;
using DTOs.Coupons;
using Repositories.Helpers;

namespace Services.Helpers.Mappers
{
    public static class CouponMapper
    {
        public static CouponDto ToDto(Coupon coupon)
        {
            return new CouponDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                Name = coupon.Name,
                Description = coupon.Description,
                Type = coupon.Type,
                Value = coupon.Value,
                MinOrderAmount = coupon.MinOrderAmount,
                MaxDiscountAmount = coupon.MaxDiscountAmount,
                UsageLimit = coupon.UsageLimit,
                UsedCount = coupon.UsedCount,
                UsageLimitPerUser = coupon.UsageLimitPerUser,
                StartDate = coupon.StartDate,
                EndDate = coupon.EndDate,
                Status = coupon.Status,
                IsFirstTimeUserOnly = coupon.IsFirstTimeUserOnly,
                CreatedAt = coupon.CreatedAt,
                UpdatedAt = coupon.UpdatedAt,
                CreatedBy = coupon.CreatedBy,
                UpdatedBy = coupon.UpdatedBy
            };
        }

        public static Coupon ToEntity(CreateCouponDto createDto)
        {
            return new Coupon
            {
                Id = Guid.NewGuid(),
                Code = createDto.Code.ToUpper().Trim(),
                Name = createDto.Name.Trim(),
                Description = createDto.Description?.Trim(),
                Type = createDto.Type,
                Value = createDto.Value,
                MinOrderAmount = createDto.MinOrderAmount,
                MaxDiscountAmount = createDto.MaxDiscountAmount,
                UsageLimit = createDto.UsageLimit,
                UsedCount = 0,
                UsageLimitPerUser = createDto.UsageLimitPerUser,
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate,
                Status = CouponStatus.Active,
                IsFirstTimeUserOnly = createDto.IsFirstTimeUserOnly
            };
        }

        public static void UpdateEntity(Coupon coupon, UpdateCouponDto updateDto)
        {
            coupon.Name = updateDto.Name.Trim();
            coupon.Description = updateDto.Description?.Trim();
            coupon.Type = updateDto.Type;
            coupon.Value = updateDto.Value;
            coupon.MinOrderAmount = updateDto.MinOrderAmount;
            coupon.MaxDiscountAmount = updateDto.MaxDiscountAmount;
            coupon.UsageLimit = updateDto.UsageLimit;
            coupon.UsageLimitPerUser = updateDto.UsageLimitPerUser;
            coupon.StartDate = updateDto.StartDate;
            coupon.EndDate = updateDto.EndDate;
            coupon.Status = updateDto.Status;
            coupon.IsFirstTimeUserOnly = updateDto.IsFirstTimeUserOnly;
        }

        public static PagedList<CouponDto> ToPagedDto(PagedList<Coupon> pagedCoupons)
        {
            var couponDtos = pagedCoupons.Select(ToDto).ToList();
            return new PagedList<CouponDto>(
                couponDtos,
                pagedCoupons.MetaData.TotalCount,
                pagedCoupons.MetaData.CurrentPage,
                pagedCoupons.MetaData.PageSize
            );
        }

        public static IEnumerable<CouponDto> ToDtoList(IEnumerable<Coupon> coupons)
        {
            return coupons.Select(ToDto);
        }
    }
}
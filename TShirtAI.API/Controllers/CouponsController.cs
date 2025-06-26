using DTOs.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public CouponsController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCoupons([FromQuery] CouponFilterDto filter)
        {
            var result = await _couponService.GetCouponsAsync(filter);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCouponById(Guid id)
        {
            var result = await _couponService.GetByIdAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("by-code/{code}")]
        public async Task<IActionResult> GetCouponByCode(string code)
        {
            var result = await _couponService.GetByCodeAsync(code);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCoupons()
        {
            var result = await _couponService.GetActiveCouponsAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("user-available/{userId:guid}")]
        public async Task<IActionResult> GetUserAvailableCoupons(Guid userId)
        {
            var result = await _couponService.GetUserAvailableCouponsAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _couponService.CreateCouponAsync(createDto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCoupon(Guid id, [FromBody] UpdateCouponDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _couponService.UpdateCouponAsync(id, updateDto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCoupon(Guid id)
        {
            var result = await _couponService.DeleteCouponAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponDto applyDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _couponService.ApplyCouponAsync(applyDto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ApplyCouponDto applyDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _couponService.ValidateCouponAsync(
                applyDto.Code, applyDto.OrderAmount, applyDto.UserId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("calculate-discount")]
        public async Task<IActionResult> CalculateDiscount([FromBody] ApplyCouponDto applyDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _couponService.CalculateDiscountAsync(
                applyDto.Code, applyDto.OrderAmount);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("use/{couponId:guid}/{userId:guid}")]
        [Authorize]
        public async Task<IActionResult> UseCoupon(Guid couponId, Guid userId)
        {
            var result = await _couponService.UseCouponAsync(couponId, userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
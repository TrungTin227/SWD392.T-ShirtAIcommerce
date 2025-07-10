using DTOs.UserCoupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/usercoupons")]
    [Authorize]
    public class UserCouponsController : ControllerBase
    {
        private readonly IUserCouponService _userCouponService;

        public UserCouponsController(IUserCouponService userCouponService)
        {
            _userCouponService = userCouponService;
        }

        /// <summary>
        /// Claim a coupon by coupon id.
        /// </summary>
        [HttpPost("claim/{id}")]
        public async Task<IActionResult> ClaimCoupon(Guid id)
        {
            var result = await _userCouponService.ClaimAsync(id);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Get all valid claimed coupons for current user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetClaimedCoupons()
        {
            var result = await _userCouponService.GetClaimedCouponsAsync();
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Unclaim (delete) one or more claimed coupons.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> UnclaimCoupons([FromBody] IEnumerable<Guid> userCouponIds)
        {
            var result = await _userCouponService.UnclaimAsync(userCouponIds);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }
}
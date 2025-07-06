using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/shipping-methods")]
    public class ShippingMethodController : ControllerBase
    {
        private readonly IShippingMethodService _shippingMethodService;
        private readonly ILogger<ShippingMethodController> _logger;

        public ShippingMethodController(
            IShippingMethodService shippingMethodService,
            ILogger<ShippingMethodController> logger)
        {
            _shippingMethodService = shippingMethodService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách phương thức vận chuyển đang hoạt động (cho người dùng chọn)
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveShippingMethods()
        {
            try
            {
                var shippingMethods = await _shippingMethodService.GetActiveShippingMethodsAsync();
                return Ok(shippingMethods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active shipping methods");
                return BadRequest("Có lỗi xảy ra khi lấy danh sách phương thức vận chuyển");
            }
        }

        /// <summary>
        /// Tính phí vận chuyển
        /// </summary>
        [HttpPost("calculate-fee")]
        public async Task<IActionResult> CalculateShippingFee([FromBody] CalculateShippingFeeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Dữ liệu không hợp lệ");
                }

                var shippingFee = await _shippingMethodService.CalculateShippingFeeAsync(
                    request.ShippingMethodId,
                    request.OrderAmount);

                return Ok(shippingFee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping fee for {@Request}", request);
                return BadRequest("Có lỗi xảy ra khi tính phí vận chuyển");
            }
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của phương thức vận chuyển
        /// </summary>
        [HttpGet("{id}/validate")]
        public async Task<IActionResult> ValidateShippingMethod(Guid id)
        {
            try
            {
                var result = await _shippingMethodService.ValidateShippingMethodAsync(id);

                if (!result.IsSuccess)
                {
                    return BadRequest(result.Message);
                }

                return Ok(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating shipping method {Id}", id);
                return BadRequest("Có lỗi xảy ra khi kiểm tra phương thức vận chuyển");
            }
        }
    }

    public class CalculateShippingFeeRequest
    {
        [Required(ErrorMessage = "ID phương thức vận chuyển là bắt buộc")]
        public Guid ShippingMethodId { get; set; }

        [Required(ErrorMessage = "Số tiền đơn hàng là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền đơn hàng phải >= 0")]
        public decimal OrderAmount { get; set; }
    }
}
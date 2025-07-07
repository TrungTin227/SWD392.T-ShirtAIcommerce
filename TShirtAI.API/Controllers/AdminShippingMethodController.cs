using DTOs.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/shipping-methods")]
    [Authorize(Roles = "Admin,Staff,Customer")]
    public class AdminShippingMethodController : ControllerBase
    {
        private readonly IShippingMethodService _shippingMethodService;
        private readonly ILogger<AdminShippingMethodController> _logger;

        public AdminShippingMethodController(
            IShippingMethodService shippingMethodService,
            ILogger<AdminShippingMethodController> logger)
        {
            _shippingMethodService = shippingMethodService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách phương thức vận chuyển (Admin)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetShippingMethods([FromQuery] ShippingMethodFilterRequest filter)
        {
            try
            {
                var result = await _shippingMethodService.GetShippingMethodsAsync(filter);

                if (!result.IsSuccess)
                {
                    return BadRequest(result.Message);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping methods");
                return BadRequest("Có lỗi xảy ra khi lấy danh sách phương thức vận chuyển");
            }
        }

        /// <summary>
        /// Lấy chi tiết phương thức vận chuyển theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetShippingMethodById(Guid id)
        {
            try
            {
                var shippingMethod = await _shippingMethodService.GetShippingMethodByIdAsync(id);

                if (shippingMethod == null)
                {
                    return BadRequest("Không tìm thấy phương thức vận chuyển");
                }

                return Ok(shippingMethod);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping method {Id}", id);
                return BadRequest("Có lỗi xảy ra khi lấy thông tin phương thức vận chuyển");
            }
        }

        /// <summary>
        /// Tạo phương thức vận chuyển mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateShippingMethod([FromBody] CreateShippingMethodRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Dữ liệu không hợp lệ");
                }

                var shippingMethod = await _shippingMethodService.CreateShippingMethodAsync(request);

                if (shippingMethod == null)
                {
                    return BadRequest("Không thể tạo phương thức vận chuyển. Có thể tên đã tồn tại hoặc dữ liệu không hợp lệ.");
                }

                return Ok(shippingMethod);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipping method {@Request}", request);
                return BadRequest("Có lỗi xảy ra khi tạo phương thức vận chuyển");
            }
        }

        /// <summary>
        /// Cập nhật phương thức vận chuyển
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateShippingMethod(Guid id, [FromBody] UpdateShippingMethodRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Dữ liệu không hợp lệ");
                }

                var updatedShippingMethod = await _shippingMethodService.UpdateShippingMethodAsync(id, request);

                if (updatedShippingMethod == null)
                {
                    return BadRequest("Không tìm thấy phương thức vận chuyển hoặc dữ liệu không hợp lệ");
                }

                return Ok(updatedShippingMethod);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shipping method {Id} with {@Request}", id, request);
                return BadRequest("Có lỗi xảy ra khi cập nhật phương thức vận chuyển");
            }
        }

        /// <summary>
        /// Xóa phương thức vận chuyển
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteShippingMethod(Guid id)
        {
            try
            {
                var result = await _shippingMethodService.DeleteShippingMethodAsync(id);

                if (!result)
                {
                    return BadRequest("Không thể xóa phương thức vận chuyển. Có thể đã được sử dụng trong đơn hàng.");
                }

                return Ok("Xóa phương thức vận chuyển thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shipping method {Id}", id);
                return BadRequest("Có lỗi xảy ra khi xóa phương thức vận chuyển");
            }
        }

        /// <summary>
        /// Bật/tắt trạng thái hoạt động của phương thức vận chuyển
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActiveStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                var result = await _shippingMethodService.ToggleActiveStatusAsync(id, isActive);

                if (!result)
                {
                    return BadRequest("Không tìm thấy phương thức vận chuyển");
                }

                return Ok($"Đã {(isActive ? "kích hoạt" : "vô hiệu hóa")} phương thức vận chuyển thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for shipping method {Id}", id);
                return BadRequest("Có lỗi xảy ra khi thay đổi trạng thái phương thức vận chuyển");
            }
        }
    }
}
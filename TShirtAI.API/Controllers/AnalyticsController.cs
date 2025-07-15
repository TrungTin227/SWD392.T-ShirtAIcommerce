using DTOs.Analytics;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [Route("api/analytics")]
    [ApiController]
    // [Authorize(Roles = "Admin,Staff")] // Bạn nên thêm phân quyền cho API này
    public class AnalyticsController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public AnalyticsController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Lấy các chỉ số phân tích chính cho dashboard.
        /// </summary>
        /// <returns>Dữ liệu tổng quan về doanh thu và đơn hàng.</returns>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(DashboardAnalyticsDto), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDashboardAnalytics()
        {
            var result = await _orderService.GetDashboardAnalyticsAsync();

            if (result != null)
            {
                return Ok(result);
            }

            // Khi service trả về null, điều đó cho biết đã có lỗi xảy ra ở tầng dưới.
            return StatusCode(500, "An internal error occurred while processing your request.");
        }
    }
}
using DTOs.OrderItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderItemController : ControllerBase
    {
        private readonly IOrderItemService _orderItemService;

        public OrderItemController(IOrderItemService orderItemService)
        {
            _orderItemService = orderItemService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff,ORDER_PROCESSOR")]
        public async Task<IActionResult> CreateOrderItem([FromBody] CreateOrderItemDto dto)
        {
            var result = await _orderItemService.CreateAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut]
        [Authorize(Roles = "Admin,Staff,ORDER_PROCESSOR")]
        public async Task<IActionResult> UpdateOrderItem([FromBody] UpdateOrderItemDto dto)
        {
            var result = await _orderItemService.UpdateAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff,ORDER_PROCESSOR")]
        public async Task<IActionResult> DeleteOrderItem(Guid id)
        {
            var result = await _orderItemService.DeleteAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderItem(Guid id)
        {
            var result = await _orderItemService.GetByIdAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderItems([FromQuery] OrderItemQueryDto query)
        {
            var result = await _orderItemService.GetOrderItemsAsync(query);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderItemsByOrderId(Guid orderId)
        {
            var result = await _orderItemService.GetByOrderIdAsync(orderId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("order/{orderId}/total")]
        public async Task<IActionResult> GetOrderTotal(Guid orderId)
        {
            var result = await _orderItemService.GetOrderTotalAsync(orderId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("order/{orderId}/count")]
        public async Task<IActionResult> GetOrderItemCount(Guid orderId)
        {
            var result = await _orderItemService.GetOrderItemCountAsync(orderId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
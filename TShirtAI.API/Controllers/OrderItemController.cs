using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

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

    /// <summary>
    /// Lấy chi tiết item trong đơn hàng
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderItem(Guid id)
    {
        var result = await _orderItemService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Lấy tất cả items trong đơn hàng
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetOrderItemsByOrderId(Guid orderId)
    {
        var result = await _orderItemService.GetByOrderIdAsync(orderId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Lấy tổng tiền của đơn hàng
    /// </summary>
    [HttpGet("order/{orderId}/total")]
    public async Task<IActionResult> GetOrderTotal(Guid orderId)
    {
        var result = await _orderItemService.GetOrderTotalAsync(orderId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
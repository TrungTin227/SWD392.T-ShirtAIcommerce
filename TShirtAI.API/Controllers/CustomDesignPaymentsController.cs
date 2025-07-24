using Azure.Core;
using DTOs.CustomOrder;
using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

[ApiController]
[Route("api/custom-design-payments")]
public class CustomDesignPaymentsController : ControllerBase
{
    private readonly ICustomDesignPaymentService _paymentService;
    private readonly IConfiguration _cfg;
    private readonly ILogger<CustomDesignPaymentsController> _logger;

    public CustomDesignPaymentsController(
        ICustomDesignPaymentService paymentService,
        IConfiguration cfg,
        ILogger<CustomDesignPaymentsController> logger)
    {
        _paymentService = paymentService;
        _cfg = cfg;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("{customDesignId:guid}")]
    public async Task<IActionResult> Create(Guid customDesignId, [FromBody] CustomDesignPaymentCreateRequest req)
    {
        req.CustomDesignId = customDesignId;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var result = await _paymentService.CreateCustomDesignPaymentAsync(req, ip);
        return Ok(new { success = true, data = result });
    }

    [AllowAnonymous]
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> VnPayReturn([FromQuery] VnPayCallbackRequest callback)
    {
        var ok = await _paymentService.HandleVnPayCallbackAsync(callback, Request);

        var baseUrl = _cfg["Frontend:BaseUrl"];
        var success = _cfg["Frontend:PaymentSuccessPath"];
        var fail = _cfg["Frontend:PaymentFailurePath"];

        var url = ok
            ? $"{baseUrl}{success}?paymentId={callback.vnp_TxnRef}&status=success&amount={callback.vnp_Amount}"
            : $"{baseUrl}{fail}?paymentId={callback.vnp_TxnRef}&status=failure&errorCode={callback.vnp_ResponseCode}";

        return Redirect(url);
    }

    [Authorize]
    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetById(Guid paymentId)
    {
        var p = await _paymentService.GetByIdAsync(paymentId);
        return p == null ? NotFound(new { success = false, message = "Payment không tồn tại" })
                         : Ok(new { success = true, data = p });
    }

    [Authorize]
    [HttpGet("by-custom/{customDesignId:guid}")]
    public async Task<IActionResult> GetByCustomDesign(Guid customDesignId)
    {
        var list = await _paymentService.GetByCustomDesignIdAsync(customDesignId);
        return Ok(new { success = true, data = list });
    }
}

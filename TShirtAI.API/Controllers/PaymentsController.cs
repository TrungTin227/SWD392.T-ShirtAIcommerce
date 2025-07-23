using BusinessObjects.Common;
using DTOs.Orders;
using DTOs.Payments;
using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implementations;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentsController> _logger;



        public PaymentsController(IPaymentService paymentService, IOrderService orderService, IConfiguration configuration
            , ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _orderService = orderService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentCreateRequest req)
        {
            if (req.PaymentMethod == PaymentMethod.VNPAY)
            {
                var vn = await _paymentService.CreateVnPayPaymentAsync(req);
                if (!vn.Success)
                    return BadRequest(new { success = false, errors = vn.Errors });

                return Ok(new
                {
                    success = true,
                    paymentId = vn.PaymentId,
                    paymentUrl = vn.PaymentUrl
                });
            }

            // COD (hoặc các method khác)
            var pay = await _paymentService.CreatePaymentAsync(req);
            return Ok(new { success = true, data = pay });
        }


        /// <summary>
        /// Endpoint để VnPay gọi về sau khi khách hàng hoàn tất thanh toán.
        /// Endpoint này sẽ xác thực và sau đó chuyển hướng người dùng về Frontend.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("vnpay/return")]
        public async Task<IActionResult> VnPayReturn([FromQuery] VnPayCallbackRequest callback)
        {
            // 1. Lấy URL của Frontend từ file cấu hình.
            var baseUrl = _configuration["Frontend:BaseUrl"];
            var successPath = _configuration["Frontend:PaymentSuccessPath"];
            var failurePath = _configuration["Frontend:PaymentFailurePath"];

            // Kiểm tra để đảm bảo cấu hình tồn tại
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(successPath) || string.IsNullOrEmpty(failurePath))
            {
                return StatusCode(500, "Server configuration error.");
            }

            string redirectUrl;
            Guid orderIdForFrontend = Guid.Empty; // Mặc định là Guid.Empty
            Guid paymentIdForFrontend = Guid.Empty;
            decimal amountForFrontend = 0;
            string statusForFrontend = "failure";

            try
            {
                // 2. Gọi service để xử lý callback và nhận về kết quả chi tiết
                var callbackResult = await _paymentService.HandleVnPayCallbackAsync(callback);

                // Lấy thông tin từ callbackResult
                orderIdForFrontend = callbackResult.OrderId ?? Guid.Empty;
                paymentIdForFrontend = callbackResult.PaymentId ?? Guid.Empty;
                amountForFrontend = callbackResult.Amount;

                // 3. Xác định trạng thái thanh toán cuối cùng để chuyển hướng
                // Sử dụng callbackResult.Success để biết backend đã xử lý và VNPAY báo thành công hay không
                if (callbackResult.Success)
                {
                    statusForFrontend = "success";
                    // Chuyển hướng về trang thành công, truyền OrderId
                    redirectUrl = $"{baseUrl}{successPath}?orderId={orderIdForFrontend}&paymentId={paymentIdForFrontend}&status={statusForFrontend}&amount={amountForFrontend}";
                }
                else
                {
                    statusForFrontend = "failure";
                    // Chuyển hướng về trang thất bại, truyền OrderId (nếu có) và mã lỗi chi tiết
                    redirectUrl = $"{baseUrl}{failurePath}?orderId={orderIdForFrontend}&paymentId={paymentIdForFrontend}&status={statusForFrontend}&vnpResponseCode={callbackResult.VnPayResponseCode}&vnpTransactionStatus={callbackResult.VnPayTransactionStatus}&message={Uri.EscapeDataString(callbackResult.Message ?? "Payment failed or unknown error.")}";
                    _logger.LogWarning("VNPAY callback failed or was not fully handled. Redirecting to: {RedirectUrl}. ResponseCode: {RespCode}, TransactionStatus: {TransStatus}",
                        redirectUrl, callbackResult.VnPayResponseCode, callbackResult.VnPayTransactionStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during VNPAY callback processing in controller. Redirecting to failure path. OrderId: {OrderId}", orderIdForFrontend);
                statusForFrontend = "error"; // Lỗi xảy ra ở backend
                redirectUrl = $"{baseUrl}{failurePath}?orderId={orderIdForFrontend}&status={statusForFrontend}&message={Uri.EscapeDataString("Internal server error during payment processing.")}";
            }

            // 5. Thực hiện chuyển hướng trình duyệt
            return Redirect(redirectUrl);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentResponse>> GetPayment(Guid id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Payment not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = payment
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetPaymentsByOrderId(Guid orderId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByOrderIdAsync(orderId);
                return Ok(new
                {
                    success = true,
                    data = payments
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("vnpay/query/{txnRef}")]
        public async Task<IActionResult> QueryVnPayPayment(string txnRef)
        {
            try
            {
                var result = await _paymentService.QueryVnPayPaymentAsync(txnRef);
                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<PaymentResponse>> UpdatePaymentStatus(
    Guid id,
    [FromBody] UpdatePaymentStatusRequest request)
        {
            try
            {
                var payment = await _paymentService.UpdatePaymentStatusAsync(
                    id,
                    request.PaymentStatus,
                    null); 

                return Ok(new
                {
                    success = true,
                    data = payment,
                    message = "Payment status updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        /// <summary>
        /// Lấy thông tin đơn hàng liên kết với payment
        /// </summary>
        [HttpGet("{paymentId}/order")]
        public async Task<IActionResult> GetOrderByPaymentId(Guid paymentId)
        {
            // 1. Lấy payment
            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
                return NotFound(new { success = false, message = "Payment không tồn tại" });

            // 2. Lấy order theo payment.OrderId
            var order = await _orderService.GetOrderByIdAsync(payment.OrderId);
            if (order == null)
                return NotFound(new { success = false, message = "Order không tìm thấy" });

            // 3. Trả về
            return Ok(new
            {
                success = true,
                data = order
            });
        }
    }
}
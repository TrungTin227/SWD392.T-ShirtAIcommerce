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



        public PaymentsController(IPaymentService paymentService, IOrderService orderService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _orderService = orderService;
            _configuration = configuration;
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

            // 1. Gọi service để xử lý callback, xác thực chữ ký và cập nhật DB.
            var isSignatureValid = await _paymentService.HandleVnPayCallbackAsync(callback);

            // 2. Lấy URL của Frontend từ file cấu hình.
            var baseUrl = _configuration["Frontend:BaseUrl"];
            var successPath = _configuration["Frontend:PaymentSuccessPath"];
            var failurePath = _configuration["Frontend:PaymentFailurePath"];

            // Kiểm tra để đảm bảo cấu hình tồn tại
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(successPath) || string.IsNullOrEmpty(failurePath))
            {
                // Trả về lỗi server nếu thiếu cấu hình, vì đây là lỗi phía server
                return StatusCode(500, "Server configuration error.");
            }

            // 3. Xác định trạng thái thanh toán cuối cùng
            var isPaymentSuccess = isSignatureValid && callback.vnp_ResponseCode == "00";

            // 4. Xây dựng URL để chuyển hướng về Frontend
            string redirectUrl;
            if (isPaymentSuccess)
            {
                // Chuyển hướng về trang thành công, đính kèm các thông tin cần thiết
                // vnp_TxnRef chứa ID của Payment, rất hữu ích cho FE
                redirectUrl = $"{baseUrl}{successPath}?paymentId={callback.vnp_TxnRef}&status=success&amount={callback.vnp_Amount}";
            }
            else
            {
                // Chuyển hướng về trang thất bại
                redirectUrl = $"{baseUrl}{failurePath}?paymentId={callback.vnp_TxnRef}&status=failure&errorCode={callback.vnp_ResponseCode}";
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
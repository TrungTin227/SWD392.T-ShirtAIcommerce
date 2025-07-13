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


        public PaymentsController(IPaymentService paymentService, IOrderService orderService)
        {
            _paymentService = paymentService;
            _orderService=orderService;
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


        [AllowAnonymous]
        [HttpGet("vnpay/return")]
        public async Task<IActionResult> VnPayReturn([FromQuery] VnPayCallbackRequest callback)
        {
            try
            {
                var isValid = await _paymentService.HandleVnPayCallbackAsync(callback);
                if (!isValid)
                    return BadRequest(new { success = false, message = "Invalid signature" });

                var respCode = callback.vnp_ResponseCode;
                var isSuccess = respCode == "00";

                return Ok(new
                {
                    success = isSuccess,
                    message = isSuccess ? "Payment successful" : "Payment failed",
                    responseCode = respCode,
                    data = isSuccess ? new
                    {
                        transactionId = callback.vnp_TransactionNo,
                        amount = callback.vnp_Amount,
                        orderInfo = callback.vnp_OrderInfo,
                        payDate = callback.vnp_CreateDate
                    } : null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
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
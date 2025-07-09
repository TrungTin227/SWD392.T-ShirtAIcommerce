using DTOs.Orders;
using DTOs.Payments;
using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentCreateRequest request)
        {
            if (string.Equals(request.PaymentMethod, "vnpay", StringComparison.OrdinalIgnoreCase))
            {
                var vnPayResponse = await _paymentService.CreateVnPayPaymentAsync(request);
                if (!vnPayResponse.Success)
                    return BadRequest(vnPayResponse);

                // Lấy URL VNPAY đã sinh ra
                var vnpUrl = vnPayResponse.PaymentUrl;
                var uri = new Uri(vnpUrl);

                // Build lại URL gọi về callback của bạn
                // Url.Action sẽ tạo: "/api/payments/vnpay/return"
                var callbackPath = Url.Action(nameof(VnPayReturn), "Payments");
                var callbackUrl = $"{Request.Scheme}://{Request.Host}{callbackPath}{uri.Query}";

                // Redirect thẳng đến VnPayReturn để bạn breakpoint và debug
                return Redirect(callbackUrl);
            }

            // xử lý phương thức thanh toán khác...
            var payment = await _paymentService.CreatePaymentAsync(request);
            return Ok(payment);
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

        [AllowAnonymous]
        [HttpGet("vnpay/return")]
        public async Task<IActionResult> VnPayReturn([FromQuery] VnPayCallbackRequest callback)
        {
            try
            {
                var isValid = await _paymentService.HandleVnPayCallbackAsync(callback);

                if (isValid)
                {
                    if (callback.vnp_ResponseCode == "00")
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Payment successful",
                            data = new
                            {
                                transactionId = callback.vnp_TransactionNo,
                                amount = callback.vnp_Amount,
                                orderInfo = callback.vnp_OrderInfo,
                                payDate = callback.vnp_PayDate
                            }
                        });
                    }
                    else
                    {
                        return Ok(new
                        {
                            success = false,
                            message = "Payment failed",
                            responseCode = callback.vnp_ResponseCode
                        });
                    }
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid signature"
                    });
                }
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
    }
}
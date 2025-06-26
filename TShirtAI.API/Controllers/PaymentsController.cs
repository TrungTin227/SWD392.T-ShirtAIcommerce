using DTOs.Payments;
using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Controllers
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
        public async Task<ActionResult<PaymentResponse>> CreatePayment([FromBody] PaymentCreateRequest request)
        {
            try
            {
                if (request.PaymentMethod.ToLower() == "vnpay")
                {
                    var vnPayResponse = await _paymentService.CreateVnPayPaymentAsync(request);

                    if (vnPayResponse.Success)
                    {
                        return Ok(new
                        {
                            success = true,
                            data = new
                            {
                                paymentUrl = vnPayResponse.PaymentUrl,
                                message = vnPayResponse.Message
                            },
                            message = "VnPay payment URL created successfully"
                        });
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = vnPayResponse.Message
                        });
                    }
                }
                else
                {
                    var payment = await _paymentService.CreatePaymentAsync(request);
                    return Ok(new
                    {
                        success = true,
                        data = payment,
                        message = "Payment created successfully"
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
                    request.Status,
                    request.TransactionId);

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

    public class UpdatePaymentStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
    }
}
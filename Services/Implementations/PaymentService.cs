using BusinessObjects.Common;
using BusinessObjects.Payments;
using DTOs.Payments;
using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Commons;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Configuration;
using Services.Extensions;
using Services.Helpers;
using Services.Interfaces;
using System.Net;

namespace Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IVnPayService _vnPayService;
        private readonly VnPayConfig _vnPayConfig;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IVnPayService vnPayService,
            IOptions<VnPayConfig> vnPayConfig,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PaymentService> logger,
            IUnitOfWork unitOfWork)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _vnPayService = vnPayService;
            _vnPayConfig = vnPayConfig.Value;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        private DateTime GetVietnamTime()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        }

        public async Task<PaymentResponse> CreatePaymentAsync(PaymentCreateRequest request)
        {
            var result = await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    // 1. Parse phương thức thanh toán
                    if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod.ToString(), true, out var paymentMethod))
                        throw new ArgumentException($"Invalid payment method: {request.PaymentMethod}");

                    // 2. Lấy đơn hàng kèm chi tiết
                    var order = await _orderRepository.GetOrderWithDetailsAsync(request.OrderId)
                                ?? throw new KeyNotFoundException($"Order {request.OrderId} not found");

                    // 3. Tính subtotal qua navigation property, rồi tổng thanh toán
                    var subtotal = order.SubtotalAmount;
                    var totalAmount = subtotal + order.ShippingFee - order.DiscountAmount;

                    // 4. Tạo đối tượng Payment mới
                    var payment = new Payment
                    {
                        OrderId = order.Id,
                        PaymentMethod = paymentMethod,
                        Amount = totalAmount,
                        // COD coi như khách đã đồng ý trả, ta mark Completed ngay
                        Status = paymentMethod == PaymentMethod.COD
                                 ? PaymentStatus.Completed
                                 : PaymentStatus.Unpaid,
                        CreatedAt = GetVietnamTime()
                    };

                    // 5. Lưu Payment
                    await _paymentRepository.AddAsync(payment);
                    await _paymentRepository.SaveChangesAsync();

                    // 6. Nếu COD, cập nhật luôn Order
                    if (paymentMethod == PaymentMethod.COD)
                    {
                        order.PaymentStatus = PaymentStatus.Unpaid;  // đã có tiền
                        order.Status = OrderStatus.Processing;
                        await _orderRepository.UpdateAsync(order);
                        await _orderRepository.SaveChangesAsync();
                    }

                    // 7. Trả về DTO
                    var response = MapToResponse(payment);
                    return ApiResult<PaymentResponse>.Success(response, "Payment created successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating payment for order {OrderId}", request.OrderId);
                    return ApiResult<PaymentResponse>.Failure($"Error creating payment: {ex.Message}");
                }
            });

            if (result.IsSuccess)
                return result.Data!;
            else
                throw new InvalidOperationException(result.Message);
        }

        public async Task<PaymentResponse?> GetPaymentByIdAsync(Guid id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            return payment != null ? MapToResponse(payment) : null;
        }

        public async Task<IEnumerable<PaymentResponse>> GetPaymentsByOrderIdAsync(Guid orderId)
        {
            var payments = await _paymentRepository.GetByOrderIdAsync(orderId);
            return payments.Select(MapToResponse);
        }

        public async Task<PaymentResponse> UpdatePaymentStatusAsync(Guid id, PaymentStatus status, string? transactionId = null)
        {
            var result = await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    var payment = await _paymentRepository.GetByIdAsync(id);
                    if (payment == null)
                        return ApiResult<PaymentResponse>.Failure("Payment not found");

                    payment.Status = status;
                    if (!string.IsNullOrEmpty(transactionId))
                        payment.TransactionId = transactionId;

                    await _paymentRepository.UpdateAsync(payment);
                    await _paymentRepository.SaveChangesAsync();

                    var response = MapToResponse(payment);
                    return ApiResult<PaymentResponse>.Success(response, "Payment status updated successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating payment status for payment {PaymentId}", id);
                    return ApiResult<PaymentResponse>.Failure($"Error updating payment status: {ex.Message}");
                }
            });

            if (result.IsSuccess)
                return result.Data!;
            else
                throw new ArgumentException(result.Message);
        }

        public async Task<PaymentResponse> UpdatePaymentStatusAsync(Guid id, string statusString, string? transactionId = null)
        {
            var result = await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    // 1. Parse string thành enum
                    if (!Enum.TryParse<PaymentStatus>(statusString, true, out var status))
                        return ApiResult<PaymentResponse>.Failure("Invalid payment status");

                    // 2. Cập nhật payment trước
                    var payment = await _paymentRepository.GetByIdAsync(id);
                    if (payment == null)
                        return ApiResult<PaymentResponse>.Failure("Payment not found");

                    payment.Status = status;
                    if (!string.IsNullOrEmpty(transactionId))
                        payment.TransactionId = transactionId;

                    await _paymentRepository.UpdateAsync(payment);
                    await _paymentRepository.SaveChangesAsync();

                    // 3. Lấy Order và cập nhật đồng bộ hai trường PaymentStatus + Status
                    var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                    if (order != null)
                    {
                        order.PaymentStatus = status;
                        if (status == PaymentStatus.Completed)
                            order.Status = OrderStatus.Paid;     // hoặc trạng thái bạn muốn khi thanh toán xong
                        else
                            order.Status = OrderStatus.Pending;       // hoặc tùy logic

                        await _orderRepository.UpdateAsync(order);
                        await _orderRepository.SaveChangesAsync();
                    }
                    else
                    {
                        // tuỳ chọn: log warning nếu không tìm thấy order
                        _logger.LogWarning("Order {OrderId} not found when syncing after payment update.", payment.OrderId);
                    }

                    var response = MapToResponse(payment);
                    return ApiResult<PaymentResponse>.Success(response, "Payment status updated successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating payment status for payment {PaymentId}", id);
                    return ApiResult<PaymentResponse>.Failure($"Error updating payment status: {ex.Message}");
                }
            });

            if (result.IsSuccess)
                return result.Data!;
            else
                throw new ArgumentException(result.Message);
        }

        public async Task<VnPayCreateResponse> CreateVnPayPaymentAsync(PaymentCreateRequest request)
        {
            var result = await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    // Lấy đơn hàng
                    var order = await _orderRepository.GetOrderWithDetailsAsync(request.OrderId);
                    if (order == null)
                        return ApiResult<VnPayCreateResponse>.Success(new VnPayCreateResponse
                        {
                            Success = false,
                            PaymentId = Guid.Empty,
                            PaymentUrl = string.Empty,
                            Payment = null,
                            Message = "Order not found"
                        }, "Order not found");

                    if (order.PaymentStatus != PaymentStatus.Unpaid)
                    {
                        return ApiResult<VnPayCreateResponse>.Success(new VnPayCreateResponse
                        {
                            Success = false,
                            PaymentId = Guid.Empty,
                            PaymentUrl = string.Empty,
                            Payment = null,
                            Message = "Đơn hàng đã được thanh toán hoặc đang xử lý, không thể thanh toán lại."
                        }, "Payment already processed");
                    }

                    var totalAmount = order.TotalAmount + order.ShippingFee - order.DiscountAmount;
                    long vnpAmountXu = Convert.ToInt64(totalAmount * 1m);

                    // Tạo payment trong DB với trạng thái Unpaid
                    var payment = new Payment
                    {
                        OrderId = request.OrderId,
                        PaymentMethod = PaymentMethod.VNPAY,
                        Amount = totalAmount,
                        Status = PaymentStatus.Unpaid,
                    };

                    await _paymentRepository.AddAsync(payment);
                    await _paymentRepository.SaveChangesAsync();

                    // Sinh các trường kỹ thuật chuẩn VNPAY
                    var vnNow = GetVietnamTime();
                    var txnRef = $"{vnNow:yyyyMMddHHmmss}_{payment.Id}";
                    var createDate = vnNow.ToString("yyyyMMddHHmmss");
                    var ipAddr = Utils.GetIpAddress(_httpContextAccessor.HttpContext!);

                    // Build request cho VNPAY Service
                    var vnPayRequest = new VnPayCreatePaymentRequest
                    {
                        vnp_TxnRef = txnRef,
                        vnp_OrderInfo = request.Description ?? $"Thanh toán đơn hàng {order.Id}",
                        vnp_OrderType = "other",
                        vnp_Amount = vnpAmountXu,
                        vnp_CreateDate = createDate,
                        vnp_IpAddr = ipAddr
                    };

                    // Xây dựng URL thanh toán VNPAY
                    var vnPayResponse = await _vnPayService.CreatePaymentUrlAsync(vnPayRequest);

                    if (vnPayResponse.Success)
                    {
                        // Cập nhật trạng thái Payment sang "Processing" và lưu TxnRef vào TransactionId
                        payment.Status = PaymentStatus.Processing;
                        payment.TransactionId = txnRef;
                        await _paymentRepository.UpdateAsync(payment);
                        await _paymentRepository.SaveChangesAsync();

                        var updatedPayment = MapToResponse(payment);

                        return ApiResult<VnPayCreateResponse>.Success(new VnPayCreateResponse
                        {
                            Success = true,
                            PaymentId = payment.Id,
                            PaymentUrl = vnPayResponse.PaymentUrl,
                            Payment = updatedPayment,
                            Message = "VnPay payment URL created successfully"
                        }, "VnPay payment URL created successfully");
                    }
                    else
                    {
                        // Trả về response với thông tin lỗi và trạng thái Payment ban đầu
                        return ApiResult<VnPayCreateResponse>.Success(new VnPayCreateResponse
                        {
                            Success = false,
                            PaymentId = payment.Id,
                            PaymentUrl = string.Empty,
                            Payment = MapToResponse(payment),
                            Message = $"Failed to create VnPay payment URL: {vnPayResponse.Message}",
                            Errors = new List<string> { vnPayResponse.Message }
                        }, "Failed to create VnPay payment URL");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating VnPay payment for order {OrderId}", request.OrderId);
                    return ApiResult<VnPayCreateResponse>.Success(new VnPayCreateResponse
                    {
                        Success = false,
                        PaymentId = Guid.Empty,
                        PaymentUrl = string.Empty,
                        Payment = null,
                        Message = $"Error creating VnPay payment: {ex.Message}",
                        Errors = new List<string> { ex.Message }
                    }, "Error creating VnPay payment");
                }
            });

            return result.Data!;
        }

        public async Task<VnPayQueryResponse> QueryVnPayPaymentAsync(string txnRef)
        {
            var vnNow = GetVietnamTime();
            var request = new VnPayQueryRequest
            {
                vnp_TxnRef = txnRef,
                vnp_OrderInfo = $"Query payment {txnRef}",
                vnp_TransDate = vnNow.ToString("yyyyMMdd"),
                vnp_CreateDate = vnNow.ToString("yyyyMMddHHmmss"),
                vnp_IpAddr = Utils.GetIpAddress(_httpContextAccessor.HttpContext!)
            };
            return await _vnPayService.QueryPaymentAsync(request);
        }

        public async Task<bool> HandleVnPayCallbackAsync(VnPayCallbackRequest callback)
        {
            var result = await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    var request = _httpContextAccessor.HttpContext!.Request;

                    // 1. Validate signature
                    if (!ValidateCallbackFromQuery(request))
                    {
                        _logger.LogError("VNPAY signature validation failed. SecureHash={Hash}",
                            request.Query["vnp_SecureHash"].ToString());
                        return ApiResult<bool>.Failure("VNPAY signature validation failed");
                    }

                    // 2. Parse paymentId từ vnp_TxnRef
                    var txnRef = callback.vnp_TxnRef;
                    if (string.IsNullOrWhiteSpace(txnRef) || !txnRef.Contains("_") ||
                        !Guid.TryParse(txnRef.Split('_')[1], out var paymentId))
                    {
                        _logger.LogError("Invalid vnp_TxnRef: {TxnRef}", txnRef);
                        return ApiResult<bool>.Failure("Invalid vnp_TxnRef");
                    }

                    // 3. Xác định trạng thái payment
                    var respCode = request.Query["vnp_ResponseCode"].ToString();
                    var transStatus = request.Query["vnp_TransactionStatus"].ToString();
                    var status = respCode == "00" && transStatus == "00"
                                 ? PaymentStatus.Completed
                                 : PaymentStatus.Failed;

                    // 4. Cập nhật bảng Payment
                    var payment = await _paymentRepository.GetByIdAsync(paymentId);
                    if (payment == null)
                    {
                        _logger.LogWarning("Payment record not found: PaymentId={PaymentId}", paymentId);
                        return ApiResult<bool>.Failure("Payment record not found");
                    }

                    // Check if payment is already completed
                    if (payment.Status == PaymentStatus.Completed)
                    {
                        _logger.LogInformation("Payment {PaymentId} already completed, skipping callback.", paymentId);
                        return ApiResult<bool>.Success(true, "Payment already completed");
                    }

                    payment.Status = status;
                    payment.TransactionId = callback.vnp_TransactionNo;
                    await _paymentRepository.UpdateAsync(payment);
                    await _paymentRepository.SaveChangesAsync();

                    // 5. Cập nhật đồng thời sang bảng Order
                    var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                    if (order != null)
                    {
                        // Cập nhật trạng thái thanh toán
                        order.PaymentStatus = status;

                        // Nếu thanh toán thành công, chuyển Order sang Confirmed
                        if (status == PaymentStatus.Completed)
                            order.Status = OrderStatus.Paid;
                        else
                            order.Status = OrderStatus.Pending; // hoặc tuỳ logic của bạn

                        await _orderRepository.UpdateAsync(order);
                        await _orderRepository.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Order not found for PaymentId={PaymentId}", paymentId);
                    }

                    _logger.LogInformation("VNPAY callback handled. PaymentId={PaymentId}, Status={Status}", paymentId, status);
                    return ApiResult<bool>.Success(true, "VNPAY callback handled successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling VNPAY callback: {@Callback}", callback);
                    return ApiResult<bool>.Failure($"Error handling VNPAY callback: {ex.Message}");
                }
            });

            return result.IsSuccess;
        }

        /// <summary>
        /// Validate VNPAY signature directly from QueryString parameters
        /// </summary>
        private bool ValidateCallbackFromQuery(HttpRequest request)
        {
            var list = request.Query
                .Where(kv => kv.Key.StartsWith("vnp_"))
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => WebUtility.UrlEncode(kv.Key) + '=' + WebUtility.UrlEncode(kv.Value));

            var rawData = string.Join("&", list);
            _logger.LogDebug("RawData for HMAC: {RawData}", rawData);

            var computed = Utils.HmacSHA512(_vnPayConfig.HashSecret, rawData);
            var received = request.Query["vnp_SecureHash"].ToString();
            _logger.LogDebug("ComputedHash={Computed} | ReceivedHash={Received}", computed, received);

            return string.Equals(computed, received, StringComparison.OrdinalIgnoreCase);
        }

        private static PaymentResponse MapToResponse(Payment payment)
            => new PaymentResponse
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                PaymentMethod = payment.PaymentMethod.ToString(),
                Amount = payment.Amount,
                TransactionId = payment.TransactionId,
                Status = payment.Status.ToString(),
                CreatedAt = payment.CreatedAt
            };
    }
}
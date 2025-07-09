using BusinessObjects.Common;
using BusinessObjects.Payments;
using DTOs.Payments;
using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Interfaces;
using Services.Configuration;
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

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IVnPayService vnPayService,
            IOptions<VnPayConfig> vnPayConfig,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _vnPayService = vnPayService;
            _vnPayConfig = vnPayConfig.Value;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private DateTime GetVietnamTime()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        }

        public async Task<PaymentResponse> CreatePaymentAsync(PaymentCreateRequest request)
        {
            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
                throw new ArgumentException("Invalid payment method");

            var order = await _orderRepository.GetOrderWithDetailsAsync(request.OrderId);
            if (order == null)
                throw new Exception("Order not found");

            var totalAmount = order.TotalAmount + order.ShippingFee - order.DiscountAmount;
            var payment = new Payment
            {
                OrderId = request.OrderId,
                PaymentMethod = paymentMethod,
                Amount = totalAmount,
                Status = PaymentStatus.Unpaid,
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            return MapToResponse(payment);
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
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                throw new ArgumentException("Payment not found");

            payment.Status = status;
            if (!string.IsNullOrEmpty(transactionId))
                payment.TransactionId = transactionId;

            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            return MapToResponse(payment);
        }

        public async Task<PaymentResponse> UpdatePaymentStatusAsync(Guid id, string statusString, string? transactionId = null)
        {
            // 1. Parse string thành enum
            if (!Enum.TryParse<PaymentStatus>(statusString, true, out var status))
                throw new ArgumentException("Invalid payment status", nameof(statusString));

            // 2. Cập nhật payment trước (gọi overload đã có)
            var paymentResponse = await UpdatePaymentStatusAsync(id, status, transactionId);

            // 3. Lấy lại bản Payment vừa cập nhật để biết OrderId
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment is null)
                throw new InvalidOperationException($"Payment {id} not found after update.");

            // 4. Lấy Order và cập nhật đồng bộ hai trường PaymentStatus + Status
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

            return paymentResponse;
        }


        public async Task<VnPayCreateResponse> CreateVnPayPaymentAsync(PaymentCreateRequest request)
        {
            try
            {
                // Lấy đơn hàng
                var order = await _orderRepository.GetOrderWithDetailsAsync(request.OrderId);
                if (order == null)
                    throw new Exception("Order not found");

                var totalAmount = order.TotalAmount + order.ShippingFee - order.DiscountAmount; //vẫn lấy được tiền
                long vnpAmountXu = Convert.ToInt64(totalAmount * 100m);
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
                    vnp_BankCode = request.BankCode ?? "",
                    vnp_Amount    = vnpAmountXu,
                    vnp_CreateDate = createDate,
                    vnp_IpAddr = ipAddr
                };

                // Xây dựng URL thanh toán VNPAY
                var vnPayResponse = await _vnPayService.CreatePaymentUrlAsync(vnPayRequest);

                if (vnPayResponse.Success)
                {
                    // Cập nhật trạng thái Payment sang "Processing" và lưu TxnRef vào TransactionId
                    var updatedPayment = await UpdatePaymentStatusAsync(payment.Id, PaymentStatus.Processing, txnRef);

                    return new VnPayCreateResponse
                    {
                        Success = true,
                        PaymentId = payment.Id,
                        PaymentUrl = vnPayResponse.PaymentUrl,
                        Payment = updatedPayment,
                        Message = "VnPay payment URL created successfully"
                    };
                }
                else
                {
                    // Trả về response với thông tin lỗi và trạng thái Payment ban đầu
                    return new VnPayCreateResponse
                    {
                        Success = false,
                        PaymentId = payment.Id,
                        PaymentUrl = string.Empty,
                        Payment = MapToResponse(payment),
                        Message = $"Failed to create VnPay payment URL: {vnPayResponse.Message}",
                        Errors = new List<string> { vnPayResponse.Message }
                    };
                }
            }
            catch (Exception ex)
            {
                return new VnPayCreateResponse
                {
                    Success = false,
                    PaymentId = Guid.Empty,
                    PaymentUrl = string.Empty,
                    Payment = null,
                    Message = $"Error creating VnPay payment: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
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
            try
            {
                var request = _httpContextAccessor.HttpContext!.Request;

                // 1. Validate signature
                if (!ValidateCallbackFromQuery(request))
                {
                    _logger.LogError("VNPAY signature validation failed. SecureHash={Hash}",
                        request.Query["vnp_SecureHash"].ToString());
                    return false;
                }

                // 2. Parse paymentId từ vnp_TxnRef
                var txnRef = callback.vnp_TxnRef;
                if (string.IsNullOrWhiteSpace(txnRef) || !txnRef.Contains("_") ||
                    !Guid.TryParse(txnRef.Split('_')[1], out var paymentId))
                {
                    _logger.LogError("Invalid vnp_TxnRef: {TxnRef}", txnRef);
                    return false;
                }

                // 3. Xác định trạng thái payment
                var respCode = request.Query["vnp_ResponseCode"].ToString();
                var transStatus = request.Query["vnp_TransactionStatus"].ToString();
                var status = respCode == "00" && transStatus == "00"
                             ? PaymentStatus.Completed
                             : PaymentStatus.Failed;

                // 4. Cập nhật bảng Payment
                await UpdatePaymentStatusAsync(paymentId, status, callback.vnp_TransactionNo);

                // 5. Cập nhật đồng thời sang bảng Order
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment != null)
                {
                    var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                    if (order != null)
                    {
                        // Cập nhật trạng thái thanh toán
                        order.PaymentStatus = status;

                        // Nếu thanh toán thành công, chuyển Order sang Confirmed
                        if (status == PaymentStatus.Completed)
                            order.Status = OrderStatus.Completed;
                        else
                            order.Status = OrderStatus.Pending; // hoặc tuỳ logic của bạn

                        await _orderRepository.UpdateAsync(order);
                        await _orderRepository.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Order not found for PaymentId={PaymentId}", paymentId);
                    }
                }
                else
                {
                    _logger.LogWarning("Payment record not found: PaymentId={PaymentId}", paymentId);
                }

                _logger.LogInformation("VNPAY callback handled. PaymentId={PaymentId}, Status={Status}", paymentId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling VNPAY callback: {@Callback}", callback);
                return false;
            }
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
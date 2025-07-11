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
        private readonly IUnitOfWork _unitOfWork;
        private readonly VnPayConfig _vnPayConfig;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IVnPayService vnPayService,
            IOptions<VnPayConfig> vnPayConfig,
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PaymentService> logger)
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
                OrderId        = order.Id,
                PaymentMethod  = paymentMethod,
                Amount         = totalAmount,
                // COD coi như khách đã đồng ý trả, ta mark Completed ngay
                Status         = paymentMethod == PaymentMethod.COD
                                 ? PaymentStatus.Completed
                                 : PaymentStatus.Unpaid,
                CreatedAt      = GetVietnamTime()
            };

            // 5. Lưu Payment
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            // 6. Nếu COD, cập nhật luôn Order
            if (paymentMethod == PaymentMethod.COD)
            {
                order.PaymentStatus = PaymentStatus.Unpaid;  // đã có tiền
                order.Status        = OrderStatus.Processing;
                await _orderRepository.UpdateAsync(order);
                await _orderRepository.SaveChangesAsync();
            }

            // 7. Trả về DTO
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


        // Thêm method này vào PaymentService
        public async Task<VnPayCreateResponse> CreateVnPayPaymentAsync(PaymentCreateRequest request)
        {
            // 1) Lấy đơn hàng ngoài transaction
            var order = await _orderRepository.GetOrderWithDetailsAsync(request.OrderId)
                        ?? throw new KeyNotFoundException($"Order {request.OrderId} not found");

            if (order.PaymentStatus != PaymentStatus.Unpaid)
                return new VnPayCreateResponse
                {
                    Success = false,
                    PaymentId = Guid.Empty,
                    PaymentUrl = string.Empty,
                    Message = "Đơn hàng đã được thanh toán hoặc đang xử lý."
                };

            // *** THÊM: Kiểm tra đã có payment VNPAY Processing chưa ***
            var existingPayment = await _paymentRepository.GetActiveVnPayPaymentByOrderIdAsync(request.OrderId);
            if (existingPayment != null)
            {
                // Nếu đã có payment processing, trả về URL cũ hoặc tạo URL mới
                var vnNow = GetVietnamTime();
                var createDate = vnNow.ToString("yyyyMMddHHmmss");
                var vnpAmountXu = Convert.ToInt64(order.TotalAmount * 1m);

                var vnPayReq = new VnPayCreatePaymentRequest
                {
                    vnp_TxnRef = existingPayment.TransactionId!,
                    vnp_OrderInfo = request.Description ?? $"Thanh toán đơn hàng {order.Id}",
                    vnp_OrderType = "other",
                    vnp_Amount = vnpAmountXu,
                    vnp_CreateDate = createDate,
                    vnp_IpAddr = Utils.GetIpAddress(_httpContextAccessor.HttpContext!)
                };

                var vnPayResp = await _vnPayService.CreatePaymentUrlAsync(vnPayReq);

                return new VnPayCreateResponse
                {
                    Success = vnPayResp.Success,
                    PaymentId = existingPayment.Id,
                    PaymentUrl = vnPayResp.PaymentUrl,
                    Payment = MapToResponse(existingPayment),
                    Message = "Sử dụng payment đã tồn tại"
                };
            }

            // 2) Tính toán số tiền, sinh txnRef, Id trước
            var totalAmount = order.TotalAmount;
            var vnNow2 = GetVietnamTime();
            var paymentId = Guid.NewGuid();
            var createDate2 = vnNow2.ToString("yyyyMMddHHmmss");
            var txnRef = $"{createDate2}_{paymentId}";
            var vnpAmountXu2 = Convert.ToInt64(totalAmount * 1m);

            // 3) Chạy trong transaction
            var apiResult = await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                // a) Tạo entity Payment, set luôn trạng thái Processing & txnRef
                var payment = new Payment
                {
                    Id = paymentId,
                    OrderId = request.OrderId,
                    PaymentMethod = PaymentMethod.VNPAY,
                    Amount = totalAmount,
                    Status = PaymentStatus.Processing,
                    TransactionId = txnRef,
                    CreatedAt = vnNow2
                };
                await _paymentRepository.AddAsync(payment);

                // b) Build request cho VnPay
                var vnPayReq = new VnPayCreatePaymentRequest
                {
                    vnp_TxnRef = txnRef,
                    vnp_OrderInfo = request.Description ?? $"Thanh toán đơn hàng {order.Id}",
                    vnp_OrderType = "other",
                    vnp_Amount = vnpAmountXu2,
                    vnp_CreateDate = createDate2,
                    vnp_IpAddr = Utils.GetIpAddress(_httpContextAccessor.HttpContext!)
                };

                // c) Gọi service VnPay
                var vnPayResp = await _vnPayService.CreatePaymentUrlAsync(vnPayReq);
                if (!vnPayResp.Success)
                {
                    // Nếu VnPay trả về lỗi, ném ra để rollback
                    throw new InvalidOperationException($"Failed to create VnPay URL: {vnPayResp.Message}");
                }

                // d) Commit 1 lần duy nhất
                await _unitOfWork.SaveChangesAsync();

                // e) Trả về DTO
                var dto = MapToResponse(payment);
                return ApiResult<VnPayCreateResponse>.Success(new VnPayCreateResponse
                {
                    Success = true,
                    PaymentId = payment.Id,
                    PaymentUrl = vnPayResp.PaymentUrl,
                    Payment = dto,
                    Message = "VnPay payment URL created successfully"
                }, "OK");
            });

            // 4) Lấy về data từ ApiResult và trả về
            return apiResult.Data!;
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
                var status = (respCode == "00" && transStatus == "00")
                             ? PaymentStatus.Completed
                             : PaymentStatus.Failed;

                // 4. Lấy Payment & Order liên quan
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment record not found: PaymentId={PaymentId}", paymentId);
                    return false;
                }

                var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                if (order == null)
                {
                    _logger.LogWarning("Order not found for PaymentId={PaymentId}", paymentId);
                    return false;
                }

                // 5. Nếu trạng thái payment chưa phải Completed → cập nhật
                if (payment.Status != PaymentStatus.Completed)
                {
                    payment.Status = status;
                    payment.TransactionId = callback.vnp_TransactionNo;
                    await _paymentRepository.UpdateAsync(payment);
                    await _paymentRepository.SaveChangesAsync();
                }

                // 6. Cập nhật trạng thái đơn hàng cho đúng
                if (order.PaymentStatus != status ||
                    (status == PaymentStatus.Completed && order.Status != OrderStatus.Paid))
                {
                    order.PaymentStatus = status;
                    order.Status = status == PaymentStatus.Completed
                        ? OrderStatus.Processing
                        : OrderStatus.Pending; // hoặc Failed, tùy hệ thống

                    await _orderRepository.UpdateAsync(order);
                    await _orderRepository.SaveChangesAsync();
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
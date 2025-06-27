using BusinessObjects.Entities.Payments;
using Configuration;
using DTOs.Payments;
using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Repositories.Interfaces;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IVnPayService _vnPayService;
        private readonly VnPayConfig _vnPayConfig;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IVnPayService vnPayService,
            IOptions<VnPayConfig> vnPayConfig,
            IHttpContextAccessor httpContextAccessor)
        {
            _paymentRepository = paymentRepository;
            _vnPayService = vnPayService;
            _vnPayConfig = vnPayConfig.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PaymentResponse> CreatePaymentAsync(PaymentCreateRequest request)
        {
            var payment = new Payment
            {
                OrderId = request.OrderId,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Status = "Pending"
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            return MapToResponse(payment);
        }

        private DateTime GetVietnamTime()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
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

        public async Task<PaymentResponse> UpdatePaymentStatusAsync(Guid id, string status, string? transactionId = null)
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

        public async Task<VnPayCreatePaymentResponse> CreateVnPayPaymentAsync(PaymentCreateRequest request)
        {
            // Create payment record first
            var payment = await CreatePaymentAsync(request);

            var txnRef = $"{DateTime.Now:yyyyMMddHHmmss}_{payment.Id}";
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var ipAddr = VnPayLibrary.GetIpAddress(_httpContextAccessor.HttpContext!);

            var vnPayRequest = new VnPayCreatePaymentRequest
            {
                vnp_TxnRef = txnRef,
                vnp_OrderInfo = request.Description ?? $"Payment for Order {request.OrderId}",
                vnp_Amount = (long)request.Amount,
                vnp_CreateDate = createDate,
                vnp_IpAddr = ipAddr,
                vnp_BankCode = request.BankCode ?? ""
            };

            var response = await _vnPayService.CreatePaymentUrlAsync(vnPayRequest);

            // Update payment with transaction ID if successful
            if (response.Success)
            {
                await UpdatePaymentStatusAsync(payment.Id, "Processing", txnRef);
            }

            return response;
        }

        public async Task<VnPayQueryResponse> QueryVnPayPaymentAsync(string txnRef)
        {
            var request = new VnPayQueryRequest
            {
                vnp_TxnRef = txnRef,
                vnp_OrderInfo = $"Query payment {txnRef}",
                vnp_TransDate = DateTime.Now.ToString("yyyyMMdd"),
                vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss"),
                vnp_IpAddr = VnPayLibrary.GetIpAddress(_httpContextAccessor.HttpContext!)
            };

            return await _vnPayService.QueryPaymentAsync(request);
        }

        public async Task<bool> HandleVnPayCallbackAsync(VnPayCallbackRequest callback)
        {
            if (!_vnPayService.ValidateCallback(callback))
                return false;

            // Extract payment ID from transaction reference
            var txnRefParts = callback.vnp_TxnRef.Split('_');
            if (txnRefParts.Length != 2 || !Guid.TryParse(txnRefParts[1], out var paymentId))
                return false;

            var status = callback.vnp_ResponseCode == "00" && callback.vnp_TransactionStatus == "00"
                ? "Completed"
                : "Failed";

            await UpdatePaymentStatusAsync(paymentId, status, callback.vnp_TransactionNo);

            return true;
        }

        private static PaymentResponse MapToResponse(Payment payment)
        {
            return new PaymentResponse
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                PaymentMethod = payment.PaymentMethod,
                Amount = payment.Amount,
                TransactionId = payment.TransactionId,
                Status = payment.Status,
                CreatedAt = payment.CreatedAt
            };
        }
    }
}
using DTOs.Payments;
using DTOs.Payments.VnPay;

namespace Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> CreatePaymentAsync(PaymentCreateRequest request);
        Task<PaymentResponse?> GetPaymentByIdAsync(Guid id);
        Task<IEnumerable<PaymentResponse>> GetPaymentsByOrderIdAsync(Guid orderId);
        Task<PaymentResponse> UpdatePaymentStatusAsync(Guid id, string status, string? transactionId = null);
        Task<VnPayCreatePaymentResponse> CreateVnPayPaymentAsync(PaymentCreateRequest request);
        Task<VnPayQueryResponse> QueryVnPayPaymentAsync(string txnRef);
        Task<bool> HandleVnPayCallbackAsync(VnPayCallbackRequest callback);
    }
}
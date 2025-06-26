using DTOs.Payments.VnPay;

namespace Services.Interfaces
{
    public interface IVnPayService
    {
        Task<VnPayCreatePaymentResponse> CreatePaymentUrlAsync(VnPayCreatePaymentRequest request);
        Task<VnPayQueryResponse> QueryPaymentAsync(VnPayQueryRequest request);
        bool ValidateCallback(VnPayCallbackRequest callback);
        string CreateQueryUrl(VnPayQueryRequest request);
    }
}
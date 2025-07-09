using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Http;

namespace Services.Interfaces
{
    public interface IVnPayService
    {
        Task<VnPayCreatePaymentResponse> CreatePaymentUrlAsync(VnPayCreatePaymentRequest request);
        Task<VnPayQueryResponse> QueryPaymentAsync(VnPayQueryRequest request);
        bool ValidateCallback(VnPayCallbackRequest callback);
        bool ValidateCallbackFromQuery(HttpRequest request);
    }
}
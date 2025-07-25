﻿using BusinessObjects.Common;
using DTOs.Payments;
using DTOs.Payments.VnPay;

namespace Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> CreatePaymentAsync(PaymentCreateRequest request);
        Task<PaymentResponse?> GetPaymentByIdAsync(Guid id);
        Task<IEnumerable<PaymentResponse>> GetPaymentsByOrderIdAsync(Guid orderId);
        Task<PaymentResponse> UpdatePaymentStatusAsync(Guid id, PaymentStatus status, string? transactionId = null);
        Task<VnPayCreateResponse> CreateVnPayPaymentAsync(PaymentCreateRequest request);
        Task<VnPayQueryResponse> QueryVnPayPaymentAsync(string txnRef);
        //Task<PaymentCallbackResult> HandleVnPayCallbackAsync(VnPayCallbackRequest callback);
        Task<bool> HandleVnPayCallbackAsync(VnPayCallbackRequest callback);
    }
}
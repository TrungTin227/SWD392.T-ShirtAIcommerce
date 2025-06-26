namespace DTOs.Payments.VnPay
{
    public class VnPayCreatePaymentResponse
    {
        public bool Success { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
namespace DTOs.Payments.VnPay
{
    public class VnPayCreateResponse
    {
        public bool Success { get; set; }
        public Guid PaymentId { get; set; }               // ← Thêm PaymentId
        public string PaymentUrl { get; set; } = string.Empty;
        public PaymentResponse Payment { get; set; }      // ← Thêm full payment info
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}

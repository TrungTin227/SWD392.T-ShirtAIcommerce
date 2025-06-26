namespace DTOs.Payments.VnPay
{
    public class VnPayQueryResponse
    {
        public string vnp_ResponseCode { get; set; } = string.Empty;
        public string vnp_Message { get; set; } = string.Empty;
        public string vnp_TmnCode { get; set; } = string.Empty;
        public string vnp_TxnRef { get; set; } = string.Empty;
        public long vnp_Amount { get; set; }
        public string vnp_OrderInfo { get; set; } = string.Empty;
        public string vnp_BankCode { get; set; } = string.Empty;
        public string vnp_PayDate { get; set; } = string.Empty;
        public string vnp_TransactionNo { get; set; } = string.Empty;
        public string vnp_TransactionType { get; set; } = string.Empty;
        public string vnp_TransactionStatus { get; set; } = string.Empty;
        public string vnp_SecureHash { get; set; } = string.Empty;
    }
}
namespace DTOs.Payments.VnPay
{
    public class VnPayCreatePaymentRequest
    {
        public string vnp_Version { get; set; } = "2.1.0";
        public string vnp_Command { get; set; } = "pay";
        public string vnp_TmnCode { get; set; } = string.Empty;
        public long vnp_Amount { get; set; }
        public string vnp_CurrCode { get; set; } = "VND";
        public string vnp_TxnRef { get; set; } = string.Empty;
        public string vnp_OrderInfo { get; set; } = string.Empty;
        public string vnp_OrderType { get; set; } = "other";
        public string vnp_Locale { get; set; } = "vn";
        public string vnp_ReturnUrl { get; set; } = string.Empty;
        public string vnp_IpAddr { get; set; } = string.Empty;
        public string vnp_CreateDate { get; set; } = string.Empty;
        public string vnp_BankCode { get; set; } = string.Empty;
        public string vnp_SecureHash { get; set; } = string.Empty;
    }
}
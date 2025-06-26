namespace DTOs.Payments.VnPay
{
    public class VnPayQueryRequest
    {
        public string vnp_Version { get; set; } = "2.1.0";
        public string vnp_Command { get; set; } = "querydr";
        public string vnp_TmnCode { get; set; } = string.Empty;
        public string vnp_TxnRef { get; set; } = string.Empty;
        public string vnp_OrderInfo { get; set; } = string.Empty;
        public string vnp_TransactionNo { get; set; } = string.Empty;
        public string vnp_TransDate { get; set; } = string.Empty;
        public string vnp_CreateDate { get; set; } = string.Empty;
        public string vnp_IpAddr { get; set; } = string.Empty;
        public string vnp_SecureHash { get; set; } = string.Empty;
    }
}
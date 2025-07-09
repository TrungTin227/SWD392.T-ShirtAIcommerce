namespace DTOs.Payments.VnPay
{
    public class VnPayQueryRequest
    {
        public string vnp_TxnRef { get; set; } = string.Empty;
        public string vnp_OrderInfo { get; set; } = string.Empty;
        public string vnp_TransDate { get; set; } = string.Empty;
        public string vnp_CreateDate { get; set; } = string.Empty;
        public string vnp_IpAddr { get; set; } = string.Empty;
    }
}
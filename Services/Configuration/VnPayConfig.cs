namespace Services.Configuration
{
    public class VnPayConfig
    {
        public string TmnCode { get; set; } = "OCW852HJ";
        public string HashSecret { get; set; } = "3TWQIXVC3Y1BZNMDPQVAD5TMNJ7K42Q6";
        public string BaseUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = "https://localhost:7266/api/Payments/vnpay/return";
        public string ApiUrl { get; set; } = string.Empty;
        public string Version { get; set; } = "2.1.0";
        public string Command { get; set; } = "pay";
        public string CurrCode { get; set; } = "VND";
        public string Locale { get; set; } = "vn";
        public string TimeZoneId { get; set; } = "SE Asia Standard Time";
    }
}
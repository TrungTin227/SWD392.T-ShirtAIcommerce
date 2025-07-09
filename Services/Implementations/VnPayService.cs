using DTOs.Payments.VnPay;
using Microsoft.Extensions.Options;
using Services.Configuration;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Implementations
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayConfig _cfg;
        private readonly HttpClient _http;

        public VnPayService(IOptions<VnPayConfig> cfg, HttpClient http)
        {
            _cfg = cfg.Value;
            _http = http;
        }

        public async Task<VnPayCreatePaymentResponse> CreatePaymentUrlAsync(VnPayCreatePaymentRequest req)
        {
            var lib = new VnPayLibrary();
            // Thêm đúng các trường chuẩn theo tài liệu
            lib.AddRequestData("vnp_Version", _cfg.Version);
            lib.AddRequestData("vnp_Command", "pay");
            lib.AddRequestData("vnp_TmnCode", _cfg.TmnCode);
            lib.AddRequestData("vnp_Amount", (req.vnp_Amount * 100).ToString());
            lib.AddRequestData("vnp_CurrCode", _cfg.CurrCode);
            lib.AddRequestData("vnp_TxnRef", req.vnp_TxnRef);
            lib.AddRequestData("vnp_OrderInfo", req.vnp_OrderInfo);
            lib.AddRequestData("vnp_OrderType", req.vnp_OrderType);
            lib.AddRequestData("vnp_Locale", req.vnp_Locale);
            lib.AddRequestData("vnp_ReturnUrl", _cfg.ReturnUrl);
            lib.AddRequestData("vnp_IpAddr", req.vnp_IpAddr);
            lib.AddRequestData("vnp_CreateDate", req.vnp_CreateDate);
            if (!string.IsNullOrEmpty(req.vnp_BankCode))
                lib.AddRequestData("vnp_BankCode", req.vnp_BankCode);

            // Build URL đúng chuẩn tài liệu
            var url = lib.CreateRequestUrl(_cfg.BaseUrl, _cfg.HashSecret);

            return new VnPayCreatePaymentResponse
            {
                Success = true,
                PaymentUrl = url,
                Message = "Create payment URL success"
            };
        }

        public async Task<VnPayQueryResponse> QueryPaymentAsync(VnPayQueryRequest req)
        {
            var lib = new VnPayLibrary();
            lib.AddRequestData("vnp_Version", _cfg.Version);
            lib.AddRequestData("vnp_Command", "querydr");
            lib.AddRequestData("vnp_TmnCode", _cfg.TmnCode);
            lib.AddRequestData("vnp_TxnRef", req.vnp_TxnRef);
            lib.AddRequestData("vnp_OrderInfo", req.vnp_OrderInfo);
            lib.AddRequestData("vnp_TransDate", req.vnp_TransDate);
            lib.AddRequestData("vnp_CreateDate", req.vnp_CreateDate);
            lib.AddRequestData("vnp_IpAddr", req.vnp_IpAddr);

            var url = lib.CreateRequestUrl(_cfg.ApiUrl, _cfg.HashSecret);
            var resp = await _http.GetStringAsync(url);

            // Parse kết quả trả về
            var dict = resp.Split('&')
                .Select(p => p.Split('=', 2))
                .Where(a => a.Length == 2)
                .ToDictionary(a => a[0], a => Uri.UnescapeDataString(a[1]));

            return new VnPayQueryResponse
            {
                vnp_ResponseCode = dict.GetValueOrDefault("vnp_ResponseCode", ""),
                vnp_Message = dict.GetValueOrDefault("vnp_Message", ""),
                vnp_TmnCode = dict.GetValueOrDefault("vnp_TmnCode", ""),
                vnp_TxnRef = dict.GetValueOrDefault("vnp_TxnRef", ""),
                vnp_Amount = long.TryParse(dict.GetValueOrDefault("vnp_Amount", "0"), out var a) ? a : 0,
                vnp_OrderInfo = dict.GetValueOrDefault("vnp_OrderInfo", ""),
                vnp_BankCode = dict.GetValueOrDefault("vnp_BankCode", ""),
                vnp_PayDate = dict.GetValueOrDefault("vnp_PayDate", ""),
                vnp_TransactionNo = dict.GetValueOrDefault("vnp_TransactionNo", ""),
                vnp_TransactionType = dict.GetValueOrDefault("vnp_TransactionType", ""),
                vnp_TransactionStatus = dict.GetValueOrDefault("vnp_TransactionStatus", ""),
                vnp_SecureHash = dict.GetValueOrDefault("vnp_SecureHash", "")
            };
        }

        public bool ValidateCallback(VnPayCallbackRequest cb)
        {
            var lib = new VnPayLibrary();

            // Thêm tất cả các trường có trong callback (trừ vnp_SecureHash và vnp_SecureHashType)
            // Thứ tự không quan trọng vì VnPayLibrary sẽ sort lại
            if (!string.IsNullOrEmpty(cb.vnp_TmnCode))
                lib.AddResponseData("vnp_TmnCode", cb.vnp_TmnCode);
            if (!string.IsNullOrEmpty(cb.vnp_Amount))
                lib.AddResponseData("vnp_Amount", cb.vnp_Amount);
            if (!string.IsNullOrEmpty(cb.vnp_BankCode))
                lib.AddResponseData("vnp_BankCode", cb.vnp_BankCode);
            if (!string.IsNullOrEmpty(cb.vnp_BankTranNo))
                lib.AddResponseData("vnp_BankTranNo", cb.vnp_BankTranNo);
            if (!string.IsNullOrEmpty(cb.vnp_CardType))
                lib.AddResponseData("vnp_CardType", cb.vnp_CardType);
            if (!string.IsNullOrEmpty(cb.vnp_OrderInfo))
                lib.AddResponseData("vnp_OrderInfo", cb.vnp_OrderInfo);
            if (!string.IsNullOrEmpty(cb.vnp_PayDate))
                lib.AddResponseData("vnp_PayDate", cb.vnp_PayDate);
            if (!string.IsNullOrEmpty(cb.vnp_ResponseCode))
                lib.AddResponseData("vnp_ResponseCode", cb.vnp_ResponseCode);
            if (!string.IsNullOrEmpty(cb.vnp_TransactionNo))
                lib.AddResponseData("vnp_TransactionNo", cb.vnp_TransactionNo);
            if (!string.IsNullOrEmpty(cb.vnp_TransactionStatus))
                lib.AddResponseData("vnp_TransactionStatus", cb.vnp_TransactionStatus);
            if (!string.IsNullOrEmpty(cb.vnp_TxnRef))
                lib.AddResponseData("vnp_TxnRef", cb.vnp_TxnRef);
            if (!string.IsNullOrEmpty(cb.vnp_SecureHashType))
                lib.AddResponseData("vnp_SecureHashType", cb.vnp_SecureHashType);

            return lib.ValidateSignature(cb.vnp_SecureHash, _cfg.HashSecret);
        }
    }
}
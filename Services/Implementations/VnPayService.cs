using DTOs.Payments.VnPay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Configuration;
using Services.Helpers;
using Services.Interfaces;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Services.Implementations
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayConfig _cfg;
        private readonly HttpClient _http;
        private readonly ILogger<VnPayService> _logger;

        public VnPayService(IOptions<VnPayConfig> cfg, HttpClient http, ILogger<VnPayService> logger)
        {
            _cfg = cfg.Value;
            _http = http;
            _logger = logger;

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
            // 1. Log toàn bộ object callback
            _logger.LogDebug("Incoming VNPAY callback content: {@Callback}", cb);



            // 1. Build map từ DTO
            var data = new SortedDictionary<string, string>();
            foreach (var prop in typeof(VnPayCallbackRequest).GetProperties())
            {
                var key = prop.Name;
                var value = prop.GetValue(cb)?.ToString();
                if (string.IsNullOrEmpty(value) ||
                    key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                    continue;

                data[key] = value;
            }

            // 2. Log từng tham số đã lấy được
            foreach (var kv in data)
            {
                _logger.LogDebug("Response param: {Key} = {Value}", kv.Key, kv.Value);
            }

            // 3. Tạo rawData và log
            var rawData = string.Join("&", data.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            _logger.LogDebug("RawData for HMAC: {RawData}", rawData);

            // 4. Tính và log chữ ký
            var computedHash = Utils.HmacSHA512(_cfg.HashSecret, rawData);
            _logger.LogDebug("ComputedHash: {Computed}  |  ReceivedHash: {Received}",
                             computedHash, cb.vnp_SecureHash);

            // 5. So sánh
            var isValid = string.Equals(computedHash, cb.vnp_SecureHash,
                                        StringComparison.OrdinalIgnoreCase);
            _logger.LogDebug("Signature valid: {IsValid}", isValid);
            return isValid;
        }
        public bool ValidateCallbackFromQuery(HttpRequest request)
        {
            // 1. Lấy tất cả param vnp_*
            var list = request.Query
                .Where(kv => kv.Key.StartsWith("vnp_"))
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv =>
                    WebUtility.UrlEncode(kv.Key)
                    + "="
                    + WebUtility.UrlEncode(kv.Value.ToString())
                );

            // 2. Tạo rawData
            var rawData = string.Join("&", list);
            _logger.LogDebug("RawData from Query: {RawData}", rawData);

            // 3. Tính HMAC-SHA512
            var computed = Utils.HmacSHA512(_cfg.HashSecret, rawData);
            var received = request.Query["vnp_SecureHash"].ToString();

            _logger.LogDebug("Computed={Computed} | Received={Received}", computed, received);

            // 4. So khớp
            return string.Equals(computed, received, StringComparison.OrdinalIgnoreCase);
        }

    }
}
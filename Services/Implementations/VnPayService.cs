using Configuration;
using DTOs.Payments.VnPay;
using Microsoft.Extensions.Options;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Implementations
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayConfig _config;
        private readonly HttpClient _httpClient;

        public VnPayService(IOptions<VnPayConfig> config, HttpClient httpClient)
        {
            _config = config.Value;
            _httpClient = httpClient;
        }

        public async Task<VnPayCreatePaymentResponse> CreatePaymentUrlAsync(VnPayCreatePaymentRequest request)
        {
            try
            {
                var vnpay = new VnPayLibrary();

                vnpay.AddRequestData("vnp_Version", _config.Version);
                vnpay.AddRequestData("vnp_Command", _config.Command);
                vnpay.AddRequestData("vnp_TmnCode", _config.TmnCode);
                vnpay.AddRequestData("vnp_Amount", (request.vnp_Amount * 100).ToString());
                vnpay.AddRequestData("vnp_CreateDate", request.vnp_CreateDate);
                vnpay.AddRequestData("vnp_CurrCode", _config.CurrCode);
                vnpay.AddRequestData("vnp_IpAddr", request.vnp_IpAddr);
                vnpay.AddRequestData("vnp_Locale", _config.Locale);
                vnpay.AddRequestData("vnp_OrderInfo", request.vnp_OrderInfo);
                vnpay.AddRequestData("vnp_OrderType", request.vnp_OrderType);
                vnpay.AddRequestData("vnp_ReturnUrl", _config.ReturnUrl);
                vnpay.AddRequestData("vnp_TxnRef", request.vnp_TxnRef);

                if (!string.IsNullOrEmpty(request.vnp_BankCode))
                {
                    vnpay.AddRequestData("vnp_BankCode", request.vnp_BankCode);
                }

                var paymentUrl = vnpay.CreateRequestUrl(_config.BaseUrl, _config.HashSecret);

                return new VnPayCreatePaymentResponse
                {
                    Success = true,
                    PaymentUrl = paymentUrl,
                    Message = "Create payment URL success"
                };
            }
            catch (Exception ex)
            {
                return new VnPayCreatePaymentResponse
                {
                    Success = false,
                    PaymentUrl = string.Empty,
                    Message = ex.Message
                };
            }
        }

        public async Task<VnPayQueryResponse> QueryPaymentAsync(VnPayQueryRequest request)
        {
            try
            {
                var queryUrl = CreateQueryUrl(request);
                var response = await _httpClient.GetAsync(queryUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse response content (VnPay returns query string format)
                var queryData = ParseQueryString(responseContent);

                return new VnPayQueryResponse
                {
                    vnp_ResponseCode = queryData.GetValueOrDefault("vnp_ResponseCode", ""),
                    vnp_Message = queryData.GetValueOrDefault("vnp_Message", ""),
                    vnp_TmnCode = queryData.GetValueOrDefault("vnp_TmnCode", ""),
                    vnp_TxnRef = queryData.GetValueOrDefault("vnp_TxnRef", ""),
                    vnp_Amount = long.TryParse(queryData.GetValueOrDefault("vnp_Amount", "0"), out var amount) ? amount : 0,
                    vnp_OrderInfo = queryData.GetValueOrDefault("vnp_OrderInfo", ""),
                    vnp_BankCode = queryData.GetValueOrDefault("vnp_BankCode", ""),
                    vnp_PayDate = queryData.GetValueOrDefault("vnp_PayDate", ""),
                    vnp_TransactionNo = queryData.GetValueOrDefault("vnp_TransactionNo", ""),
                    vnp_TransactionType = queryData.GetValueOrDefault("vnp_TransactionType", ""),
                    vnp_TransactionStatus = queryData.GetValueOrDefault("vnp_TransactionStatus", ""),
                    vnp_SecureHash = queryData.GetValueOrDefault("vnp_SecureHash", "")
                };
            }
            catch (Exception ex)
            {
                return new VnPayQueryResponse
                {
                    vnp_ResponseCode = "99",
                    vnp_Message = ex.Message
                };
            }
        }

        public bool ValidateCallback(VnPayCallbackRequest callback)
        {
            try
            {
                var vnpay = new VnPayLibrary();

                vnpay.AddResponseData("vnp_TmnCode", callback.vnp_TmnCode);
                vnpay.AddResponseData("vnp_Amount", callback.vnp_Amount);
                vnpay.AddResponseData("vnp_BankCode", callback.vnp_BankCode);
                vnpay.AddResponseData("vnp_BankTranNo", callback.vnp_BankTranNo);
                vnpay.AddResponseData("vnp_CardType", callback.vnp_CardType);
                vnpay.AddResponseData("vnp_PayDate", callback.vnp_PayDate);
                vnpay.AddResponseData("vnp_OrderInfo", callback.vnp_OrderInfo);
                vnpay.AddResponseData("vnp_TransactionNo", callback.vnp_TransactionNo);
                vnpay.AddResponseData("vnp_ResponseCode", callback.vnp_ResponseCode);
                vnpay.AddResponseData("vnp_TransactionStatus", callback.vnp_TransactionStatus);
                vnpay.AddResponseData("vnp_TxnRef", callback.vnp_TxnRef);
                vnpay.AddResponseData("vnp_SecureHashType", callback.vnp_SecureHashType);

                return vnpay.ValidateSignature(callback.vnp_SecureHash, _config.HashSecret);
            }
            catch
            {
                return false;
            }
        }

        public string CreateQueryUrl(VnPayQueryRequest request)
        {
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", _config.Version);
            vnpay.AddRequestData("vnp_Command", "querydr");
            vnpay.AddRequestData("vnp_TmnCode", _config.TmnCode);
            vnpay.AddRequestData("vnp_TxnRef", request.vnp_TxnRef);
            vnpay.AddRequestData("vnp_OrderInfo", request.vnp_OrderInfo);
            vnpay.AddRequestData("vnp_TransactionNo", request.vnp_TransactionNo);
            vnpay.AddRequestData("vnp_TransDate", request.vnp_TransDate);
            vnpay.AddRequestData("vnp_CreateDate", request.vnp_CreateDate);
            vnpay.AddRequestData("vnp_IpAddr", request.vnp_IpAddr);

            return vnpay.CreateRequestUrl(_config.ApiUrl, _config.HashSecret);
        }

        private static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>();
            var pairs = queryString.Split('&');

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    result[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
                }
            }

            return result;
        }
    }
}
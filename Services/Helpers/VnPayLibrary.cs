using System.Net;
using System.Text;

namespace Services.Helpers
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _requestData[key] = value;
        }
        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _responseData[key] = value;
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            // Build query string (sort key, encode value)
            var data = new StringBuilder();
            foreach (var kv in _requestData)
            {
                data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
            var querystring = data.ToString();
            if (querystring.EndsWith("&")) querystring = querystring[..^1];

            // Sinh secure hash
            var vnpSecureHash = Utils.HmacSHA512(vnpHashSecret, querystring);
            var fullUrl = $"{baseUrl}?{querystring}&vnp_SecureHash={vnpSecureHash}";
            return fullUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var data = new StringBuilder();
            var list = _responseData
                .Where(x => x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
                .ToList();
            foreach (var kv in list)
            {
                data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
            if (data.Length > 0) data.Remove(data.Length - 1, 1);
            var myChecksum = Utils.HmacSHA512(secretKey, data.ToString());
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
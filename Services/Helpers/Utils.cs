using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace Services.Helpers
{
    public static class Utils
    {
        /// <summary>
        /// Sinh HMAC-SHA512 của inputData với key.
        /// </summary>
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var b in hashValue)
                {
                    hash.Append(b.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        /// <summary>
        /// Lấy địa chỉ IP client từ HttpContext (ưu tiên header, fallback RemoteIpAddress).
        /// </summary>
        public static string GetIpAddress(HttpContext context)
        {
            try
            {
                // 1) X-Forwarded-For
                if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var fwd) && !string.IsNullOrWhiteSpace(fwd))
                {
                    return fwd.ToString().Split(',').First().Trim();
                }
                // 2) X-Real-IP
                if (context.Request.Headers.TryGetValue("X-Real-IP", out var real) && !string.IsNullOrWhiteSpace(real))
                {
                    return real.ToString();
                }
                // 3) CF-Connecting-IP
                if (context.Request.Headers.TryGetValue("CF-Connecting-IP", out var cf) && !string.IsNullOrWhiteSpace(cf))
                {
                    return cf.ToString();
                }

                // 4) Fallback RemoteIpAddress
                var remoteIp = context.Connection.RemoteIpAddress;
                if (remoteIp != null)
                {
                    // nếu IPv6 loopback, chuyển thành 127.0.0.1
                    if (remoteIp.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // thử lấy IPv4 nếu có
                        var entry = Dns.GetHostEntry(remoteIp);
                        var ipv4 = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                        if (ipv4 != null) remoteIp = ipv4;
                    }
                    var ip = remoteIp.ToString();
                    return ip == "::1" ? "127.0.0.1" : ip;
                }
            }
            catch (Exception ex)
            {
                // nếu có lỗi, trả về thông báo
                return $"Invalid IP: {ex.Message}";
            }

            // mặc định
            return "127.0.0.1";
        }
    }

    /// <summary>
    /// So sánh chuỗi theo mã codepoint Ordinal (để sort key ổn định).
    /// </summary>
    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return StringComparer.Ordinal.Compare(x, y);
        }
    }
}

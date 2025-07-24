using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CustomOrder
{
    namespace DTOs.CustomOrder
    {
        public class CustomDesignVnPayCreateRequest
        {
            public string vnp_TxnRef { get; set; } = string.Empty;
            public string vnp_OrderInfo { get; set; } = string.Empty;
            public long vnp_Amount { get; set; }
            public string vnp_OrderType { get; set; } = "other";
            public string vnp_Locale { get; set; } = "vn";
            public string vnp_IpAddr { get; set; } = "";
            public string vnp_CreateDate { get; set; } = "";
            public string? vnp_BankCode { get; set; }
            public string? vnp_ReturnUrl { get; set; }
        }
    }


}

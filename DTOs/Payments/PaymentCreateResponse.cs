using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.Payments
{
    public class PaymentCreateResponse
    {
        public bool Success { get; set; }
        public Guid? PaymentId { get; set; }
        public string? PaymentUrl { get; set; }            // Chỉ có với VnPay
        public PaymentResponse? Payment { get; set; }      // Full payment info
        public bool RequiresRedirect { get; set; }        // True = cần redirect, False = xử lý local
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}

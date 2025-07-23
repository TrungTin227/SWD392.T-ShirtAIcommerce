using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CustomOrder
{
    public class CustomDesignPaymentResponse
    {
        public Guid PaymentId { get; set; }
        public Guid CustomDesignId { get; set; }
        public string PaymentMethod { get; set; } = "";
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Notes { get; set; }

        // Nếu dùng VNPAY
        public string? PaymentUrl { get; set; }
        public string? VnPayResponseCode { get; set; }
    }
}

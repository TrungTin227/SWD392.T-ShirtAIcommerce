using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.Payments
{
    public class PaymentCallbackResult
    {
        public bool Success { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string? VnPayResponseCode { get; set; }
        public string? VnPayTransactionStatus { get; set; }
        public string? Message { get; set; }
    }
}

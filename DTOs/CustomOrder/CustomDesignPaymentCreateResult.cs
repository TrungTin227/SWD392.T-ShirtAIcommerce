using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CustomOrder
{
    public class CustomDesignPaymentCreateResult
    {
        public bool Success { get; set; }
        public Guid PaymentId { get; set; }
        public Guid CustomDesignId { get; set; }
        public string PaymentMethod { get; set; } = default!;
        public decimal Amount { get; set; }
        public string OrderDescription { get; set; } = default!;
        public string TransactionId { get; set; } = default!;
        public string? PaymentUrl { get; set; }   
        public string Message { get; set; } = default!;
    }
}

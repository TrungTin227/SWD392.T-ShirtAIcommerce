using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CustomOrder
{
    public class CustomDesignPaymentCreateRequest
    {
        [Required]
        public Guid CustomDesignId { get; set; }
        [Required]
        public PaymentMethod PaymentMethod { get; set; } // Enum: VNPAY, COD, etc.
        public string? Description { get; set; }

        public string? PayerName { get; set; }
        public string? PayerPhone { get; set; }
        public string? PayerAddress { get; set; }
    }
}
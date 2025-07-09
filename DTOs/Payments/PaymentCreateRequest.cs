using BusinessObjects.Common;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Payments
{
    public class PaymentCreateRequest
    {
        [Required]
        public Guid OrderId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } 
        public string? Description { get; set; }
    }
}
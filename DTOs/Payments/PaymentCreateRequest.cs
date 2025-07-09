using System.ComponentModel.DataAnnotations;

namespace DTOs.Payments
{
    public class PaymentCreateRequest
    {
        [Required]
        public Guid OrderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string? BankCode { get; set; } // For VnPay bank selection
    }
}
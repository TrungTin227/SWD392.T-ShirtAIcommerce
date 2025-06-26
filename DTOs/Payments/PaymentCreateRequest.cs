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

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public string? BankCode { get; set; } // For VnPay bank selection
    }
}
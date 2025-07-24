using BusinessObjects.CustomDesigns;
using BusinessObjects.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.CustomDesignPayments
{
    public class CustomDesignPayment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CustomDesignId { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? TransactionId { get; set; }  // Mã giao dịch VNPAY trả về

        public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [ForeignKey("CustomDesignId")]
        public virtual CustomDesign CustomDesign { get; set; } = null!;

        [MaxLength(255)]
        public string? PayerName { get; set; }

        [MaxLength(20)]
        public string? PayerPhone { get; set; }

        [MaxLength(500)]
        public string? PayerAddress { get; set; }

    }
}

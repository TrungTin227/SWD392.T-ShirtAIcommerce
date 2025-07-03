using BusinessObjects.Orders;
using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Payments
{
    public class Payment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
    
        public Guid OrderId { get; set; }

        [Required]
        [MaxLength(50)]
        public PaymentMethod PaymentMethod { get; set; } // VNPAY, COD

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? TransactionId { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
    }
}
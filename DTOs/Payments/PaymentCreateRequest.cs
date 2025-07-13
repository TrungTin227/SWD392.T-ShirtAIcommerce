using BusinessObjects.Common;
using BusinessObjects.Orders;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Payments
{
    public class PaymentCreateRequest
    {
        [Required]
        public Guid OrderId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } 
        public string? Description { get; set; }
        public Order? Order { get; set; }  // Truyền Order object thay vì query từ DB

    }
}
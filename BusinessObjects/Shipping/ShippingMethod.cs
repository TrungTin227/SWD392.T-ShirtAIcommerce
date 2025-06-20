using BusinessObjects.Identity;
using BusinessObjects.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Shipping
{
    public class ShippingMethod : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Tên phương thức vận chuyển là bắt buộc")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Phí vận chuyển là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Phí vận chuyển phải >= 0")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Fee { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(12,2)")]
        public decimal? FreeShippingThreshold { get; set; }

        [Range(1, 365, ErrorMessage = "Thời gian giao hàng từ 1-365 ngày")]
        public int EstimatedDays { get; set; }

        [Range(1, 365)]
        public int? MinDeliveryDays { get; set; }

        [Range(1, 365)]
        public int? MaxDeliveryDays { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(0, int.MaxValue)]
        public int SortOrder { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
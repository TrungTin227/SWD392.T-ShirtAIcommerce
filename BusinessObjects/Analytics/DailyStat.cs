using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Analytics
{
    public class DailyStat
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public DateOnly ReportDate { get; set; }

        public int TotalOrders { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalRevenue { get; set; } = 0;

        public int NewCustomers { get; set; } = 0;
    }
}
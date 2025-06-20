using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Identity
{
    public class UserAddress
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [MaxLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc")]
        [MaxLength(500)]
        public string DetailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phường/Xã là bắt buộc")]
        [MaxLength(100)]
        public string Ward { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quận/Huyện là bắt buộc")]
        [MaxLength(100)]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc")]
        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
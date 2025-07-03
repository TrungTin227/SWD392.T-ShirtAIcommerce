using System.ComponentModel.DataAnnotations;

namespace DTOs.UserAddressDTOs.Request
{
    public class UpdateUserAddressRequest
    {
        public Guid Id { get; set; }

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
        public bool IsDefault { get; set; } = false;
    }
}
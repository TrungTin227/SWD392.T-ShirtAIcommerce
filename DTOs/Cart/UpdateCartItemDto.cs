using System.ComponentModel.DataAnnotations;

namespace DTOs.Cart
{
    public class UpdateCartItemDto
    {
        [MaxLength(50)]
        public string? SelectedColor { get; set; }

        [MaxLength(20)]
        public string? SelectedSize { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng từ 1-100")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Đơn giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 0")]
        public decimal UnitPrice { get; set; }
    }
}
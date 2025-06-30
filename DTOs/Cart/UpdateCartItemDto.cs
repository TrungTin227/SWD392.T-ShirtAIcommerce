using BusinessObjects.Products; 
using System.ComponentModel.DataAnnotations;

namespace DTOs.Cart
{
    public class UpdateCartItemDto
    {
        // Use enums instead of string
        public ProductColor? SelectedColor { get; set; }
        public ProductSize? SelectedSize { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng từ 1-100")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Đơn giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 0")]
        public decimal UnitPrice { get; set; }
    }
}
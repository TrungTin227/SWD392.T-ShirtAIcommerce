using BusinessObjects.Products; 
using System.ComponentModel.DataAnnotations;

namespace DTOs.Cart
{
    public class UpdateCartItemDto
    {

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng từ 1-100")]
        public int Quantity { get; set; }
    }
}
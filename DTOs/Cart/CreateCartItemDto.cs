using System.ComponentModel.DataAnnotations;
using BusinessObjects.Products; 

namespace DTOs.Cart
{
    public class CreateCartItemDto
    {
        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }

        // Use enums instead of string
        public ProductColor? SelectedColor { get; set; }
        public ProductSize? SelectedSize { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng từ 1-100")]
        public int Quantity { get; set; } = 1;
    }
}
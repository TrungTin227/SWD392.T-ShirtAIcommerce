using System.ComponentModel.DataAnnotations;

namespace DTOs.Cart
{
    public class CreateCartItemDto
    {
        public Guid? ProductVariantId { get; set; }
        public Guid? CustomDesignId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng từ 1-100")]
        public int Quantity { get; set; } = 1;
    }
}
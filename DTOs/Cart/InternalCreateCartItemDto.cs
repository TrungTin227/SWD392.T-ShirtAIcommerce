using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Cart
{
    /// <summary>
    /// Internal DTO used between Controller and Service layers
    /// Contains UserId/SessionId set by Controller
    /// </summary>
    public class InternalCreateCartItemDto
    {
        public Guid? UserId { get; set; }

        [MaxLength(255)]
        public string? SessionId { get; set; }

        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }

        // Use enums instead of string
        public ProductColor? SelectedColor { get; set; }
        public ProductSize? SelectedSize { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng từ 1-100")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Đơn giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 0")]
        public decimal UnitPrice { get; set; }
    }
}
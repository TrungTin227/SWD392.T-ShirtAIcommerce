using System.ComponentModel.DataAnnotations;
using DTOs.UserAddressDTOs.Request;

namespace DTOs.Orders
{
    public class CreateOrderRequest
    {
        /// <summary>
        /// Nếu chọn địa chỉ đã lưu, truyền UserAddressId
        /// </summary>
        public Guid? UserAddressId { get; set; }

        /// <summary>
        /// Nếu muốn nhập địa chỉ mới, truyền đầy đủ thông tin tại đây
        /// </summary>
        public CreateUserAddressRequest? NewAddress { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? CustomerNotes { get; set; }

        public Guid? CouponId { get; set; }
        public Guid? ShippingMethodId { get; set; }

        [Required(ErrorMessage = "Sản phẩm trong đơn hàng là bắt buộc")]
        [MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất 1 sản phẩm")]
        public List<CreateOrderItemRequest> OrderItems { get; set; } = new();

        /// <summary>
        /// Rule: 
        /// - Nếu UserAddressId có giá trị thì NewAddress phải null (và ngược lại).
        /// - Nếu tạo từ cart thì mỗi OrderItem chỉ cần truyền CartItemId.
        /// - Nếu thêm sản phẩm ngoài cart thì truyền đủ thông tin sản phẩm.
        /// </summary>
    }

    public class CreateOrderItemRequest
    {
        /// <summary>
        /// Nếu tạo từ cart thì truyền CartItemId và không cần truyền các trường dưới.
        /// Nếu không, truyền đủ thông tin sản phẩm.
        /// </summary>
        public Guid? CartItemId { get; set; }

        // Các trường dưới chỉ required nếu không có CartItemId

        public Guid? ProductId { get; set; }
        public Guid? CustomDesignId { get; set; }
        public Guid? ProductVariantId { get; set; }

        [MaxLength(255)]
        public string? ItemName { get; set; }
        public BusinessObjects.Products.ProductColor? SelectedColor { get; set; }
        public BusinessObjects.Products.ProductSize? SelectedSize { get; set; }

        [Range(1, int.MaxValue)]
        public int? Quantity { get; set; }
    }
}
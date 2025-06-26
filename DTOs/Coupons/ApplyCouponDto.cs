using System.ComponentModel.DataAnnotations;

namespace DTOs.Coupons
{
    public class ApplyCouponDto
    {
        [Required(ErrorMessage = "Mã coupon là bắt buộc")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số tiền đơn hàng là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền đơn hàng phải lớn hơn 0")]
        public decimal OrderAmount { get; set; }

        public Guid? UserId { get; set; }
    }
}
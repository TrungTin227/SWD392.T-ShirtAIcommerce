using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Coupons
{
    public class UpdateCouponDto
    {
        [Required(ErrorMessage = "Tên coupon là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tên coupon không được vượt quá 255 ký tự")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Loại coupon là bắt buộc")]
        public CouponType Type { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0")]
        public decimal Value { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền đơn hàng tối thiểu phải >= 0")]
        public decimal? MinOrderAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm tối đa phải >= 0")]
        public decimal? MaxDiscountAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải >= 1")]
        public int? UsageLimit { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng trên mỗi người dùng phải >= 1")]
        public int? UsageLimitPerUser { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public CouponStatus Status { get; set; }

        public bool IsFirstTimeUserOnly { get; set; } = false;
    }
}
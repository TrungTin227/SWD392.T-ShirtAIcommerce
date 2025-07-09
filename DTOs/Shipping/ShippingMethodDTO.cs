using BusinessObjects.Common;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Shipping
{
    public class ShippingMethodDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Fee { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public int EstimatedDays { get; set; }
        public int? MinDeliveryDays { get; set; }
        public int? MaxDeliveryDays { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public string? UpdatedByName { get; set; }
    }

    public class CreateShippingMethodRequest
    {
        [Required(ErrorMessage = "Tên phương thức vận chuyển là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Phí vận chuyển là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Phí vận chuyển phải >= 0")]
        public decimal Fee { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Ngưỡng miễn phí vận chuyển phải >= 0")]
        public decimal? FreeShippingThreshold { get; set; }

        [Required(ErrorMessage = "Thời gian giao hàng ước tính là bắt buộc")]
        [Range(1, 365, ErrorMessage = "Thời gian giao hàng từ 1-365 ngày")]
        public int EstimatedDays { get; set; }

        [Range(1, 365, ErrorMessage = "Thời gian giao hàng tối thiểu từ 1-365 ngày")]
        public int? MinDeliveryDays { get; set; }

        [Range(1, 365, ErrorMessage = "Thời gian giao hàng tối đa từ 1-365 ngày")]
        public int? MaxDeliveryDays { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(0, int.MaxValue, ErrorMessage = "Thứ tự sắp xếp phải >= 0")]
        public int SortOrder { get; set; } = 0;
    }

    public class UpdateShippingMethodRequest
    {
        [Required]
        public ShippingCategory Name { get; set; }

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Phí vận chuyển phải >= 0")]
        public decimal? Fee { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Ngưỡng miễn phí vận chuyển phải >= 0")]
        public decimal? FreeShippingThreshold { get; set; }

        [Range(1, 365, ErrorMessage = "Thời gian giao hàng từ 1-365 ngày")]
        public int? EstimatedDays { get; set; }

        [Range(1, 365, ErrorMessage = "Thời gian giao hàng tối thiểu từ 1-365 ngày")]
        public int? MinDeliveryDays { get; set; }

        [Range(1, 365, ErrorMessage = "Thời gian giao hàng tối đa từ 1-365 ngày")]
        public int? MaxDeliveryDays { get; set; }

        public bool? IsActive { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Thứ tự sắp xếp phải >= 0")]
        public int? SortOrder { get; set; }
    }

    public class ShippingMethodFilterRequest
    {
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        public decimal? MinFee { get; set; }
        public decimal? MaxFee { get; set; }
        public int? MinEstimatedDays { get; set; }
        public int? MaxEstimatedDays { get; set; }
        public string? SortBy { get; set; } = "SortOrder";
        public bool SortDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ShippingMethodQueryDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public decimal? MinFee { get; set; }
        public decimal? MaxFee { get; set; }
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = false;
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
    }
}
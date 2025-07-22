using System.ComponentModel.DataAnnotations;

namespace DTOs.Orders
{
    public class RequestCancellationRequest
    {
        [Required(ErrorMessage = "Lý do yêu cầu hủy là bắt buộc.")]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}

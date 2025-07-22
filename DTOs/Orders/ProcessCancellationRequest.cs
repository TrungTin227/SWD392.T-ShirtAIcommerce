using BusinessObjects.Common;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Orders
{
    public class ProcessCancellationRequest
    {
        [Required(ErrorMessage = "Trạng thái xử lý yêu cầu hủy là bắt buộc.")]
        public CancellationRequestStatus Status { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú của Admin không được vượt quá 1000 ký tự.")]
        public string? AdminNotes { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DTOs.Common
{
    public class BatchIdsRequest
    {
        [Required(ErrorMessage = "Danh sách ID không được rỗng")]
        [MinLength(1, ErrorMessage = "Phải có tối thiểu 1 ID")]
        public List<Guid> Ids { get; set; } = new();
    }
    public class BatchOperationErrorDTO
    {
        public string Id { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
    public class BatchOperationResultDTO
    {
        public int TotalRequested { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> SuccessIds { get; set; } = new();
        public List<BatchOperationErrorDTO> Errors { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool IsPartialSuccess => SuccessCount > 0 && FailureCount > 0;
        public bool IsCompleteSuccess => SuccessCount == TotalRequested && FailureCount == 0;
        public bool IsCompleteFailure => SuccessCount == 0 && FailureCount > 0;
    }
    public class ErrorResponse
    {
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
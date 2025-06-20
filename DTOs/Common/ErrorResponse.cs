namespace DTOs.Common
{
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
}
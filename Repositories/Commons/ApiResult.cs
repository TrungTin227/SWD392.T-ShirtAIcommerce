namespace Repositories.Commons
{
    public record ApiResult<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public string? Message { get; init; }
        public Exception? Exception { get; init; }

        private ApiResult(bool isSuccess, T? data, string? message, Exception? exception = null)
        {
            IsSuccess = isSuccess;
            Data = data;
            Message = message;
            Exception = exception;
        }

        // Success methods
        public static ApiResult<T> Success(T data) =>
            new(true, data, null);

        public static ApiResult<T> Success(T data, string message) =>
            new(true, data, message);

        // Failure methods
        public static ApiResult<T> Failure(string message) =>
            new(false, default, message);

        public static ApiResult<T> Failure(string message, Exception exception) =>
            new(false, default, message, exception);

        public static ApiResult<T> Failure(Exception exception) =>
            new(false, default, exception.Message, exception);

        // Error methods (for cases where you might want to return partial data)
        public static ApiResult<T> Error(T? data, string message) =>
            new(false, data, message);

        public static ApiResult<T> Error(T? data, Exception exception) =>
            new(false, data, exception.Message, exception);

        // Implicit conversion operators for easier usage
        public static implicit operator bool(ApiResult<T> result) => result.IsSuccess;

        public static implicit operator T?(ApiResult<T> result) => result.Data;

        // Helper method to handle results
        public TResult Match<TResult>(Func<T?, TResult> onSuccess, Func<string?, Exception?, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(Data) : onFailure(Message, Exception);
        }
    }

    // Non-generic version for operations that don't return data
    public record ApiResult
    {
        public bool IsSuccess { get; init; }
        public string? Message { get; init; }
        public Exception? Exception { get; init; }

        private ApiResult(bool isSuccess, string? message, Exception? exception = null)
        {
            IsSuccess = isSuccess;
            Message = message;
            Exception = exception;
        }

        public static ApiResult Success() =>
            new(true, null);

        public static ApiResult Success(string message) =>
            new(true, message);

        public static ApiResult Failure(string message) =>
            new(false, message);

        public static ApiResult Failure(string message, Exception exception) =>
            new(false, message, exception);

        public static ApiResult Failure(Exception exception) =>
            new(false, exception.Message, exception);

        public static implicit operator bool(ApiResult result) => result.IsSuccess;

        // Convert to generic ApiResult
        public ApiResult<T> To<T>() =>
            IsSuccess ? ApiResult<T>.Success(default(T)!, Message ?? string.Empty)
                      : ApiResult<T>.Failure(Message ?? string.Empty, Exception);
    }
}
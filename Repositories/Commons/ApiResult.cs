namespace Repositories.Commons
{
    public class ApiResult<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public string? Message { get; init; }
        public Exception? Exception { get; init; }

        private ApiResult(bool isSuccess, T? data = default, string? message = null, Exception? exception = null)
        {
            IsSuccess = isSuccess;
            Data = data;
            Message = message;
            Exception = exception;
        }

        // ✔ Success
        public static ApiResult<T> Success(T data, string? message = null) =>
            new(true, data, message);

        // ❌ Failure
        public static ApiResult<T> Failure(string message, Exception? exception = null) =>
            new(false, default, message, exception);

        public static ApiResult<T> Failure(Exception exception) =>
            new(false, default, exception.Message, exception);

        // ⚠ Error with partial data
        public static ApiResult<T> Error(T? data, string message, Exception? exception = null) =>
            new(false, data, message, exception);

        public static ApiResult<T> Error(T? data, Exception exception) =>
            new(false, data, exception.Message, exception);

        // 🎯 Functional-style matching
        public TResult Match<TResult>(
            Func<T?, TResult> onSuccess,
            Func<string?, Exception?, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(Data) : onFailure(Message, Exception);
        }

        // 🔄 Implicit conversions
        public static implicit operator bool(ApiResult<T> result) => result.IsSuccess;
        public static implicit operator T?(ApiResult<T> result) => result.Data;
    }
    public record ApiResult
    {
        public bool IsSuccess { get; init; }
        public string? Message { get; init; }
        public Exception? Exception { get; init; }

        private ApiResult(bool isSuccess, string? message = null, Exception? exception = null)
        {
            IsSuccess = isSuccess;
            Message = message;
            Exception = exception;
        }

        // ✔ Success
        public static ApiResult Success(string? message = null) =>
            new(true, message);

        // ❌ Failure
        public static ApiResult Failure(string message, Exception? exception = null) =>
            new(false, message, exception);

        public static ApiResult Failure(Exception exception) =>
            new(false, exception.Message, exception);

        // 🔄 Implicit conversion
        public static implicit operator bool(ApiResult result) => result.IsSuccess;

        // 🔁 Convert to generic
        public ApiResult<T> To<T>(T? data = default) =>
            IsSuccess
                ? ApiResult<T>.Success(data!, Message)
                : ApiResult<T>.Failure(Message ?? "Unknown error", Exception);
    }
}

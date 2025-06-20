using Repositories.Commons;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace WebAPI.Middlewares
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Decide status code and error message based on exception
            var (statusCode, errorMessage) = exception switch
            {
                ValidationException validationEx => ((int)HttpStatusCode.BadRequest, validationEx.Message),
                UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Unauthorized access"),
                KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Resource not found"),
                ArgumentException argEx => ((int)HttpStatusCode.BadRequest, argEx.Message),
                _ => ((int)HttpStatusCode.InternalServerError, "An internal server error occurred")
            };

            context.Response.StatusCode = statusCode;

            // Construct the response with all needed properties at once
            var response = ApiResult<object>.Failure(errorMessage, exception);

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private static string GetErrorMessage(Exception exception)
        {
            return exception switch
            {
                ValidationException => exception.Message,
                ArgumentException => exception.Message,
                KeyNotFoundException => "Resource not found",
                UnauthorizedAccessException => "Unauthorized access",
                _ => "An error occurred while processing your request"
            };
        }
    }

    // Extension method for easy registration
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }
    }
}
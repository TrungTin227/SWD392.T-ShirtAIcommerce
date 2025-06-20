using Repositories;

namespace WebAPI.Middlewares
{
    public class DbContextTransactionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DbContextTransactionMiddleware> _logger;

        public DbContextTransactionMiddleware(RequestDelegate next, ILogger<DbContextTransactionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, T_ShirtAIcommerceContext dbContext)
        {
            // Skip for GET requests as they usually don't modify data
            if (context.Request.Method == HttpMethods.Get)
            {
                await _next(context);
                return;
            }

            // For POST, PUT, DELETE requests, use transaction
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                await _next(context);

                // Only commit if response is successful
                if (context.Response.StatusCode < 400)
                {
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during request processing. Rolling back transaction.");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
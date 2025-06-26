using Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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
            // Simply pass through - let services handle their own transactions
            await _next(context);
        }
    }
}
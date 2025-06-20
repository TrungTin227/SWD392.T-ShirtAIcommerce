using BusinessObjects.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace WebAPI.Middlewares
{
    public class SecurityStampValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityStampValidationMiddleware> _logger;

        public SecurityStampValidationMiddleware(RequestDelegate next, ILogger<SecurityStampValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
                {
                    var user = await userManager.FindByIdAsync(userGuid.ToString());
                    if (user == null)
                    {
                        // User not found, clear authentication
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("User not found");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
using BusinessObjects.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repositories;
using WebAPI.Middlewares;

namespace WebAPI.Extensions
{
    public static class PipelineConfigurationExtensions
    {
        public static async Task<IApplicationBuilder> UseApplicationPipeline(this IApplicationBuilder app)
        {
            app.UseGlobalExceptionHandling();

            // 1. Database Migration and Seeding with improved error handling
            var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var db = scope.ServiceProvider.GetRequiredService<T_ShirtAIcommerceContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

                try
                {
                    // FIX: Use async migration with timeout
                    await db.Database.MigrateAsync();

                    // FIX: Initialize database with proper error handling
                    await DBInitializer.Initialize(db, userManager, roleManager);

                    logger.LogInformation("Database migration and seeding completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error when migrating/seeding database. Application will continue but may have issues.");
                    // Don't throw here to allow app to start even if seeding fails
                }

                if (env.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }
            }

            // 2. Common Middleware
            app.UseHttpsRedirection();
            app.UseRouting();

            // 2.1. Enable CORS
            app.UseCors("CorsPolicy");

            // 2.2. Add header for Google OAuth popup
            app.Use(async (context, next) =>
            {
                context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
                await next();
            });

            // 2.3. Authentication & Authorization
            app.UseAuthentication();
            app.UseMiddleware<SecurityStampValidationMiddleware>();

            // FIX: Add transaction middleware for data consistency
            app.UseMiddleware<DbContextTransactionMiddleware>();

            app.UseAuthorization();

            // 3. Endpoints
            app.UseEndpoints(endpoints => endpoints.MapControllers());

            return app;
        }
    }
}
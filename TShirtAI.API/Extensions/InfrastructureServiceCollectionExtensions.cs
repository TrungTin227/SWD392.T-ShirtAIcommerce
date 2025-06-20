using BusinessObjects.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Repositories;
using Repositories.Implementations;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;
using Repositories.WorkSeeds.Interfaces;
using Services.Implementations;
using Services.Interfaces;
using System.Security.Claims;
using System.Text;
using Services.Commons.Gmail.Implementations;

namespace WebAPI.Extensions
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Cấu hình Settings
            services.Configure<DTOs.UserDTOs.Identities.JwtSettings>(configuration.GetSection("JwtSettings"));
            //services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddHttpContextAccessor();

            // 2. DbContext và CORS
            services.AddDbContext<T_ShirtAIcommerceContext>(opt =>
                opt.UseSqlServer(
                    configuration.GetConnectionString("T_ShirtAIcommerceContext"),
                    sql => sql.MigrationsAssembly("Repositories")));

            services.AddCors(opt =>
            {
                opt.AddPolicy("CorsPolicy", b => b
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

            // 3. Identity & Authentication
            services.AddIdentity<ApplicationUser, ApplicationRole>(opts =>
            {
                // Bắt buộc phải xác thực email mới cho SignIn
                opts.SignIn.RequireConfirmedEmail = true;

                opts.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;
                opts.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
                opts.Lockout.MaxFailedAccessAttempts = 5;
                opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                opts.Lockout.AllowedForNewUsers = true;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequireDigit = false;
                opts.Password.RequireLowercase = false;
                opts.Password.RequireUppercase = false;
                opts.Password.RequiredLength = 4;
            })
            .AddEntityFrameworkStores<T_ShirtAIcommerceContext>()
            .AddDefaultTokenProviders();

            var jwt = configuration.GetSection("JwtSettings").Get<DTOs.UserDTOs.Identities.JwtSettings>()
                      ?? throw new InvalidOperationException("JWT key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.ValidIssuer,
                    ValidAudience = jwt.ValidAudience,
                    IssuerSigningKey = key
                };
                // Custom error handling
                opts.Events = new JwtBearerEvents
                {
                    OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "application/json";
                        var res = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "You are not authorized. Please authenticate."
                        });
                        return ctx.Response.WriteAsync(res);
                    }
                };
            });

            // 4. Repositories & Domain Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICurrentTime, CurrentTime>();
            services.AddScoped<IRepositoryFactory, RepositoryFactory>();

            // Business Services - đặt sau khi đã có HttpContextAccessor
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ITokenService, TokenService>();
            //services.AddScoped<IUserService, UserService>();

            // 5. Email + Quartz
            services.AddEmailServices(configuration.GetSection("EmailSettings"));

            // 6. Controllers
            services.AddControllers();

            return services;
        }
    }
}
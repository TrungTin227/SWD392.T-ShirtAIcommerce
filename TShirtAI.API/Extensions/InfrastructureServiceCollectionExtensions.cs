using BusinessObjects.Identity;
using Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Repositories;
using Repositories.Implementations;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons.Gmail.Implementations;
using Services.Implementations;
using Services.Implements;
using Services.Interfaces;
using Services.Interfaces.Services.Commons.User;
using System.Security.Claims;
using System.Text;

namespace WebAPI.Extensions
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // === 0. Cấu hình Cache + Session (nếu cần) ===
            services.AddDistributedMemoryCache();
            services.AddSession(opts =>
            {
                opts.Cookie.Name = ".TShirtAICommerce.Session";
                opts.IdleTimeout = TimeSpan.FromHours(1);
                opts.Cookie.HttpOnly = true;
            });

            // === 1. Cấu hình Settings ===
            services.Configure<DTOs.UserDTOs.Identities.JwtSettings>(
                configuration.GetSection("JwtSettings"));
            services.Configure<VnPayConfig>(
                configuration.GetSection("VnPay"));
            services.AddHttpContextAccessor();

            // === 2. EF Core DbContext ===
            services.AddDbContext<T_ShirtAIcommerceContext>(opt =>
                opt.UseSqlServer(
                    configuration.GetConnectionString("T_ShirtAIcommerceContext"),
                    sql => sql.MigrationsAssembly("Repositories")));

            // === 3. CORS ===
            services.AddCors(opt =>
                opt.AddPolicy("CorsPolicy", b => b
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyMethod()
                    .AllowAnyHeader()));

            // === 4. Identity & Authentication ===
            services.AddIdentity<ApplicationUser, ApplicationRole>(opts =>
            {
                opts.SignIn.RequireConfirmedEmail = true;
                opts.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;
                opts.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
                opts.Lockout.MaxFailedAccessAttempts = 5;
                opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                opts.Password.RequiredLength = 4;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequireDigit = false;
                opts.Password.RequireLowercase = false;
                opts.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<T_ShirtAIcommerceContext>()
            .AddDefaultTokenProviders();

            var jwtSection = configuration.GetSection("JwtSettings");
            var jwt = jwtSection.Get<DTOs.UserDTOs.Identities.JwtSettings>()
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
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"];
            });

            // === 5. HTTP Clients (Ví dụ VnPay) ===
            services.AddHttpClient<IVnPayService, VnPayService>();

            // === 6. Repositories & Domain Services ===
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped<IRepositoryFactory, RepositoryFactory>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserAddressRepository, UserAddressRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<ICouponRepository, CouponRepository>();
            services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
            services.AddScoped<ICartItemRepository, CartItemRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<ICustomDesignRepository, CustomDesignRepository>();
            services.AddScoped<IWishlistRepository, WishlistRepository>();
            services.AddScoped<IProductVariantRepository, ProductVariantRepository>();

            services.AddScoped<ICurrentTime, CurrentTime>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IExternalAuthService, ExternalAuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserEmailService, UserEmailService>();
            services.AddScoped<IUserAddressService, UserAddressService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IOrderItemService, OrderItemService>();
            services.AddScoped<IShippingMethodService, ShippingMethodService>();
            services.AddScoped<ICartItemService, CartItemService>();
            services.AddScoped<IVnPayService, VnPayService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<ICustomDesignService, CustomDesignService>();
            services.AddScoped<IWishlistService, WishlistService>();
            services.AddScoped<IProductVariantService, ProductVariantService>();

            // === 7. Email & Quartz ===
            services.AddEmailServices(configuration.GetSection("EmailSettings"));

            // === 8. Controllers ===
            services.AddControllers();

            return services;
        }
    }
}

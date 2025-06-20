using BusinessObjects.Analytics;
using BusinessObjects.Cart;
using BusinessObjects.CustomDesigns;
using BusinessObjects.Entities.AI;
using BusinessObjects.Entities.Payments;
using BusinessObjects.Identity;
using BusinessObjects.Orders;
using BusinessObjects.Products;
using BusinessObjects.Reviews;
using BusinessObjects.Coupons;
using BusinessObjects.Shipping;
using BusinessObjects.Wishlists;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BusinessObjects.Comparisons;
using Microsoft.AspNetCore.Http; // ✅ Add this
using System.Security.Claims;    // ✅ Add this

namespace Repositories
{
    public class T_ShirtAIcommerceContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        private readonly IHttpContextAccessor _httpContextAccessor; // ✅ Change this

        public T_ShirtAIcommerceContext(DbContextOptions<T_ShirtAIcommerceContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor; // ✅ Change this
        }

        // Existing DbSets
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CustomDesign> CustomDesigns { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ProductComparison> ProductComparisons { get; set; }
        public DbSet<AiRecommendation> AiRecommendations { get; set; }
        public DbSet<DailyStat> DailyStats { get; set; }

        // 🆕 New DbSets - CƠ BẢN CHO 5 TUẦN
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }
        public DbSet<ShippingMethod> ShippingMethods { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ... all your existing OnModelCreating code ...
            base.OnModelCreating(modelBuilder);

            // 🔧 CHỈ GIỮ LẠI CÁC CONFIG CẦN THIẾT

            // CustomDesign relationships - FIX lỗi multiple foreign key
            modelBuilder.Entity<CustomDesign>(entity =>
            {
                entity.HasOne(cd => cd.User)
                      .WithMany(u => u.CustomDesigns)
                      .HasForeignKey(cd => cd.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cd => cd.Staff)
                      .WithMany(u => u.StaffDesigns)
                      .HasForeignKey(cd => cd.StaffId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Order relationships - FIX lỗi multiple foreign key
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasOne(o => o.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.AssignedStaff)
                      .WithMany(u => u.AssignedOrders)
                      .HasForeignKey(o => o.AssignedStaffId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 🆕 Composite Keys - CẦN THIẾT
            modelBuilder.Entity<UserCoupon>()
                .HasKey(uc => new { uc.UserId, uc.CouponId });

            modelBuilder.Entity<WishlistItem>()
                .HasKey(wi => new { wi.UserId, wi.ProductId });

            // 💰 Decimal precision - CẦN THIẾT
            modelBuilder.Entity<Product>().Property(e => e.Price).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<Product>().Property(e => e.SalePrice).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<Product>().Property(e => e.Weight).HasColumnType("decimal(6,2)");
            modelBuilder.Entity<Product>().Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");

            modelBuilder.Entity<CustomDesign>().Property(e => e.TotalPrice).HasColumnType("decimal(12,2)");

            modelBuilder.Entity<Order>().Property(e => e.TotalAmount).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<Order>().Property(e => e.ShippingFee).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<Order>().Property(e => e.DiscountAmount).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<Order>().Property(e => e.TaxAmount).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<Order>().Property(e => e.RefundAmount).HasColumnType("decimal(12,2)");

            modelBuilder.Entity<OrderItem>().Property(e => e.UnitPrice).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<OrderItem>().Property(e => e.TotalPrice).HasColumnType("decimal(12,2)");

            modelBuilder.Entity<CartItem>().Property(e => e.UnitPrice).HasColumnType("decimal(12,2)");

            modelBuilder.Entity<Payment>().Property(e => e.Amount).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<DailyStat>().Property(e => e.TotalRevenue).HasColumnType("decimal(12,2)");

            modelBuilder.Entity<ProductVariant>().Property(e => e.PriceAdjustment).HasColumnType("decimal(12,2)");

            modelBuilder.Entity<Coupon>().Property(e => e.Value).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<Coupon>().Property(e => e.MinOrderAmount).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<Coupon>().Property(e => e.MaxDiscountAmount).HasColumnType("decimal(12,2)");

            modelBuilder.Entity<ShippingMethod>().Property(e => e.Fee).HasColumnType("decimal(12,2)");
            modelBuilder.Entity<ShippingMethod>().Property(e => e.FreeShippingThreshold).HasColumnType("decimal(12,2)");

            // 📇 Unique indexes - CẦN THIẾT
            modelBuilder.Entity<Order>().HasIndex(e => e.OrderNumber).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(e => e.Sku).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(e => e.Slug).IsUnique();
            modelBuilder.Entity<ProductVariant>().HasIndex(e => e.VariantSku).IsUnique();
            modelBuilder.Entity<Coupon>().HasIndex(e => e.Code).IsUnique();

            // 🆕 Enum conversions - ĐƠN GIẢN
            modelBuilder.Entity<Product>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<Order>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<Order>().Property(e => e.PaymentStatus).HasConversion<string>();
            modelBuilder.Entity<CustomDesign>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<CustomDesign>().Property(e => e.Size).HasConversion<string>();
            modelBuilder.Entity<CustomDesign>().Property(e => e.LogoPosition).HasConversion<string>();
            modelBuilder.Entity<Review>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<Coupon>().Property(e => e.Type).HasConversion<string>();
            modelBuilder.Entity<Coupon>().Property(e => e.Status).HasConversion<string>();

            // ✅ Existing configs
            modelBuilder.Entity<Product>().Property(e => e.CreatedBy).IsRequired(false);
            modelBuilder.Entity<Product>().Property(e => e.UpdatedBy).IsRequired(false);
            modelBuilder.Entity<Category>().Property(e => e.CreatedBy).IsRequired(false);
            modelBuilder.Entity<Category>().Property(e => e.UpdatedBy).IsRequired(false);

            // 🗑️ Soft delete - BASIC
            modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CustomDesign>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Review>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Coupon>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ShippingMethod>().HasQueryFilter(e => !e.IsDeleted);
        }

        // Auto audit cho BaseEntity
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            var currentUserId = GetCurrentUserId();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.CreatedBy = currentUserId;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = currentUserId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = currentUserId;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = DateTime.UtcNow;
                        entry.Entity.DeletedBy = currentUserId;
                        break;
                }
            }
        }

        private Guid? GetCurrentUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return null;

            if (Guid.TryParse(userIdString, out var userId))
                return userId;

            return null;
        }
    }
}
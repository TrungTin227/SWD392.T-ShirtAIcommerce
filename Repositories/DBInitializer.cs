using BusinessObjects.Coupons;
using BusinessObjects.Identity;
using BusinessObjects.Products;
using BusinessObjects.Shipping;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public static class DBInitializer
    {
        public static async Task Initialize(T_ShirtAIcommerceContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            await SeedRolesAsync(roleManager);
            await SeedUsersAsync(context, userManager);
            await SeedCategoriesAsync(context);
            await SeedProductsAsync(context);
            await SeedShippingMethodsAsync(context);
            await SeedCouponsAsync(context);
        }

        #region Seed Roles

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            var roles = new List<string> { "Admin", "Staff", "Customer" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper()
                    };
                    await roleManager.CreateAsync(role);
                }
            }
        }

        #endregion

        #region Seed Users

        private static async Task SeedUsersAsync(T_ShirtAIcommerceContext context, UserManager<ApplicationUser> userManager)
        {
            var systemUserId = Guid.NewGuid(); // For CreatedBy field

            // Admin User
            if (await userManager.FindByNameAsync("admin") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "tinvtse@gmail.com",
                    EmailConfirmed = true,
                    FirstName = "System",
                    LastName = "Administrator",
                    Gender = Gender.Other,
                    PhoneNumber = "0901234567",
                    PhoneNumberConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = systemUserId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = systemUserId,
                    IsDeleted = false
                };

                var result = await userManager.CreateAsync(admin, "string");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    Console.WriteLine("Admin user created successfully");
                }
            }

            // Staff Users
            var staffUsers = new List<(string username, string email, string firstName, string lastName, string address, Gender gender)>
            {
                ("staff1", "staff1@tshirtai.com", "Nguyen Van", "Staff", "456 Staff Street, Ho Chi Minh City", Gender.Male),
                ("staff2", "staff2@tshirtai.com", "Tran Thi", "Designer", "789 Design Avenue, Ho Chi Minh City", Gender.Female),
                ("staff3", "trungtin2272002@gmail.com", "Le Van", "Production", "321 Production Road, Ho Chi Minh City", Gender.Male),
                ("designmanager", "designmanager@tshirtai.com", "Pham Thi", "Manager", "555 Manager Boulevard, Ho Chi Minh City", Gender.Female)
            };

            foreach (var (username, email, firstName, lastName, address, gender) in staffUsers)
            {
                if (await userManager.FindByNameAsync(username) == null)
                {
                    var staff = new ApplicationUser
                    {
                        UserName = username,
                        Email = email,
                        EmailConfirmed = true,
                        FirstName = firstName,
                        LastName = lastName,
                        Gender = gender,
                        PhoneNumber = $"090{Random.Shared.Next(1000000, 9999999)}",
                        PhoneNumberConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = systemUserId,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = systemUserId,
                        IsDeleted = false
                    };

                    var result = await userManager.CreateAsync(staff, "string");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(staff, "Staff");
                        Console.WriteLine($"Staff user {username} created successfully");
                    }
                }
            }

            // Customer Users (including TrungTin227)
            var customerUsers = new List<(string username, string email, string firstName, string lastName, string address, Gender gender)>
            {
                ("TrungTin227", "trungtin227@gmail.com", "Trung", "Tin", "227 Developer Street, Thu Duc City", Gender.Male),
                ("customer1", "customer1@gmail.com", "Pham Van", "Customer", "111 Customer Street, District 1", Gender.Male),
                ("customer2", "customer2@gmail.com", "Hoang Thi", "Buyer", "222 Buyer Avenue, District 3", Gender.Female),
                ("fashionlover", "fashionlover@gmail.com", "Nguyen", "Fashion Lover", "444 Style Boulevard, District 2", Gender.Female),
                ("designcreator", "designcreator@gmail.com", "Vo Van", "Designer", "333 Creative Road, District 7", Gender.Male),
                ("tshirtfan", "tinvtse161572@fpt.edu.vn", "Le Thi", "Fashion", "666 Fashion Street, District 5", Gender.Female),
                ("customerloyalty", "loyalty@gmail.com", "Tran Van", "Loyalty", "777 Loyalty Avenue, District 10", Gender.Male),
                ("youngcustomer", "young@gmail.com", "Nguyen Thi", "Young", "888 Youth Road, District 8", Gender.Female)
            };

            foreach (var (username, email, firstName, lastName, address, gender) in customerUsers)
            {
                if (await userManager.FindByNameAsync(username) == null)
                {
                    var customer = new ApplicationUser
                    {
                        UserName = username,
                        Email = email,
                        EmailConfirmed = true,
                        FirstName = firstName,
                        LastName = lastName,
                        Gender = gender,
                        PhoneNumber = $"090{Random.Shared.Next(1000000, 9999999)}",
                        PhoneNumberConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = systemUserId,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = systemUserId,
                        IsDeleted = false
                    };

                    var result = await userManager.CreateAsync(customer, "string");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(customer, "Customer");
                        Console.WriteLine($"Customer user {username} created successfully");
                    }
                }
            }

            // Update security stamps for all users
            var allUsers = await context.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                if (string.IsNullOrEmpty(user.SecurityStamp))
                {
                    await userManager.UpdateSecurityStampAsync(user);
                    Console.WriteLine($"Security stamp updated for user {user.UserName}");
                }
            }
        }

        #endregion

        #region Seed Categories

        private static async Task SeedCategoriesAsync(T_ShirtAIcommerceContext context)
        {
            if (!await context.Categories.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var categories = new List<Category>
                {
                    new Category
                    {
                        Name = "T-Shirt",
                        Description = "Basic cotton t-shirts for everyday wear",
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Products = new List<Product>
                        {
                            new Product
                            {
                                Name = "Classic White T-Shirt",
                                Description = "100% cotton, unisex, comfortable fit.",
                                Price = 150000,
                                CreatedAt = now,
                                UpdatedAt = now
                            },
                            new Product
                            {
                                Name = "Black Graphic T-Shirt",
                                Description = "Trendy graphic print, regular fit.",
                                Price = 180000,
                                CreatedAt = now,
                                UpdatedAt = now
                            }
                        }
                    },
                    new Category
                    {
                        Name = "Polo",
                        Description = "Polo shirts for casual and semi-formal occasions",
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Products = new List<Product>
                        {
                            new Product
                            {
                                Name = "Classic Blue Polo",
                                Description = "Cotton blend, ribbed collar.",
                                Price = 220000,
                                CreatedAt = now,
                                UpdatedAt = now
                            },
                            new Product
                            {
                                Name = "Slim Fit Polo",
                                Description = "Modern fit, breathable fabric.",
                                Price = 250000,
                                CreatedAt = now,
                                UpdatedAt = now
                            }
                        }
                    },
                    new Category
                    {
                        Name = "Hoodie",
                        Description = "Hooded sweatshirts for cold weather",
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Products = new List<Product>
                        {
                            new Product
                            {
                                Name = "Grey Zip-up Hoodie",
                                Description = "Soft fleece, front pockets.",
                                Price = 350000,
                                CreatedAt = now,
                                UpdatedAt = now
                            },
                            new Product
                            {
                                Name = "Black Pullover Hoodie",
                                Description = "Warm interior, adjustable hood.",
                                Price = 370000,
                                CreatedAt = now,
                                UpdatedAt = now
                            }
                        }
                    },
                    new Category
                    {
                        Name = "Tank Top",
                        Description = "Sleeveless shirts for summer",
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Products = new List<Product>
                        {
                            new Product
                            {
                                Name = "White Tank Top",
                                Description = "Lightweight, perfect for summer.",
                                Price = 120000,
                                CreatedAt = now,
                                UpdatedAt = now
                            },
                            new Product
                            {
                                Name = "Black Sport Tank Top",
                                Description = "Quick-dry, great for gym.",
                                Price = 140000,
                                CreatedAt = now,
                                UpdatedAt = now
                            }
                        }
                    },
                    new Category
                    {
                        Name = "Long Sleeve",
                        Description = "Long-sleeved shirts for cooler weather",
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Products = new List<Product>
                        {
                            new Product
                            {
                                Name = "Plain Long Sleeve",
                                Description = "Soft fabric, basic style.",
                                Price = 200000,
                                CreatedAt = now,
                                UpdatedAt = now
                            },
                            new Product
                            {
                                Name = "Striped Long Sleeve",
                                Description = "Trendy stripes, snug fit.",
                                Price = 220000,
                                CreatedAt = now,
                                UpdatedAt = now
                            }
                        }
                    }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
                Console.WriteLine("Categories and sample Products seeded successfully");
            }
        }

        #endregion

        #region Seed Products

        private static async Task SeedProductsAsync(T_ShirtAIcommerceContext context)
        {
            // Nếu chưa có bất kỳ variant nào, seed toàn bộ products + variants
            if (!await context.ProductVariants.AnyAsync())
            {
                var categories = await context.Categories.ToListAsync();
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
                if (adminUser == null)
                {
                    Console.WriteLine("Admin user not found. Skipping product seeding.");
                    return;
                }

                // Helper: sinh đúng 5 variants và gắn vào navigation collection
                List<ProductVariant> GenerateVariants(Product product, string baseSku,
                                                     List<string> colors, List<string> sizes, int quantity = 20)
                {
                    var variants = new List<ProductVariant>();
                    var combos = colors.SelectMany(c => sizes.Select(s => (color: c, size: s)))
                                       .Take(5);

                    foreach (var (color, size) in combos)
                    {
                        variants.Add(new ProductVariant
                        {
                            Id = Guid.NewGuid(),
                            Color = Enum.Parse<ProductColor>(color),
                            Size = Enum.Parse<ProductSize>(size),
                            VariantSku = $"{baseSku}-{color.ToUpper()}-{size.ToUpper()}",
                            Quantity = quantity,
                            PriceAdjustment = 0m,
                            ImageUrl = $"/images/products/{baseSku.ToLower()}-{color.ToLower()}-{size.ToLower()}.jpg",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = adminUser.Id,
                            UpdatedBy = adminUser.Id,
                            IsDeleted = false,
                            // Không gán ProductId — EF sẽ tự điền khi save thông qua navigation:
                            Product = product
                        });
                    }

                    return variants;
                }

                var productsToSeed = new List<Product>();

                // Danh sách các category cần seed cùng với cấu hình variant
                var seedDefinitions = new[]
                {
            // T-Shirt
            new {
                CategoryName = "T-Shirt",
                Products = new[]
                {
                    new {
                        Name = "Basic Cotton T-Shirt White",
                        Sku = "TSHIRT-WHITE-001",
                        Description = "100% cotton basic t-shirt in white color. Perfect for custom designs.",
                        Price = 150000m, SalePrice = 120000m,
                        Material = ProductMaterial.Cotton100,
                        Season = ProductSeason.AllSeason,
                        Colors = new List<string>{ "White","Black","Navy","Gray","Red" }
                    },
                    new {
                        Name = "Basic Cotton T-Shirt Black",
                        Sku = "TSHIRT-BLACK-002",
                        Description = "100% cotton basic t-shirt in black color. Classic and versatile.",
                        Price = 150000m, SalePrice = 120000m,
                        Material = ProductMaterial.Cotton100,
                        Season = ProductSeason.AllSeason,
                        Colors = new List<string>{ "Black","White","Navy","Gray","Red" }
                    },
                    new {
                        Name = "Graphic T-Shirt Vintage",
                        Sku = "TSHIRT-VINTAGE-003",
                        Description = "Vintage style graphic t-shirt with retro design. Soft cotton blend.",
                        Price = 180000m, SalePrice = 150000m,
                        Material = ProductMaterial.CottonSpandex,
                        Season = ProductSeason.AllSeason,
                        Colors = new List<string>{ "Gray","Black","White","Navy","Maroon" }
                    }
                }
            },
            // Polo
            new {
                CategoryName = "Polo",
                Products = new[]
                {
                    new {
                        Name = "Classic Polo Shirt Blue",
                        Sku = "POLO-BLUE-001",
                        Description = "Cotton blend polo shirt with ribbed collar. Professional and comfortable.",
                        Price = 220000m, SalePrice = 180000m,
                        Material = ProductMaterial.Bamboo,
                        Season = ProductSeason.AllSeason,
                        Colors = new List<string>{ "Blue","White","Black","Navy","Gray" }
                    },
                    new {
                        Name = "Premium Polo Shirt White",
                        Sku = "POLO-WHITE-002",
                        Description = "Premium cotton polo with elegant design. Perfect for business casual.",
                        Price = 250000m, SalePrice = 200000m,
                        Material = ProductMaterial.Cotton100,
                        Season = ProductSeason.AllSeason,
                        Colors = new List<string>{ "White","Blue","Black","Gray","Green" }
                    }
                }
            },
            // Hoodie
            new {
                CategoryName = "Hoodie",
                Products = new[]
                {
                    new {
                        Name = "Premium Hoodie Gray",
                        Sku = "HOODIE-GRAY-001",
                        Description = "Premium fleece hoodie with front pockets. Perfect for cold weather.",
                        Price = 350000m, SalePrice = 300000m,
                        Material = ProductMaterial.Cotton100,
                        Season = ProductSeason.Winter,
                        Colors = new List<string>{ "Gray","Black","Navy","White","Maroon" }
                    },
                    new {
                        Name = "Sport Hoodie Black",
                        Sku = "HOODIE-SPORT-002",
                        Description = "Athletic hoodie with moisture-wicking fabric. Great for workouts.",
                        Price = 380000m, SalePrice = 320000m,
                        Material = ProductMaterial.Polyester,
                        Season = ProductSeason.Autumn,
                        Colors = new List<string>{ "Black","Gray","Navy","Blue","Red" }
                    }
                }
            },
            // Tank Top
            new {
                CategoryName = "Tank Top",
                Products = new[]
                {
                    new {
                        Name = "Summer Tank Top White",
                        Sku = "TANK-WHITE-001",
                        Description = "Lightweight cotton tank top. Perfect for hot summer days.",
                        Price = 120000m, SalePrice = 100000m,
                        Material = ProductMaterial.Cotton100,
                        Season = ProductSeason.Summer,
                        Colors = new List<string>{ "White","Black","Gray","Navy","Blue" }
                    },
                    new {
                        Name = "Athletic Tank Top Black",
                        Sku = "TANK-SPORT-002",
                        Description = "Performance tank top with quick-dry technology. Ideal for gym and sports.",
                        Price = 140000m, SalePrice = 120000m,
                        Material = ProductMaterial.Polyester,
                        Season = ProductSeason.Summer,
                        Colors = new List<string>{ "Black","White","Gray","Red","Blue" }
                    }
                }
            },
            // Long Sleeve
            new {
                CategoryName = "Long Sleeve",
                Products = new[]
                {
                    new {
                        Name = "Basic Long Sleeve White",
                        Sku = "LONG-WHITE-001",
                        Description = "Comfortable long sleeve shirt in soft cotton. Great for layering.",
                        Price = 200000m, SalePrice = 170000m,
                        Material = ProductMaterial.Cotton100,
                        Season = ProductSeason.Autumn,
                        Colors = new List<string>{ "White","Black","Gray","Navy","Green" }
                    },
                    new {
                        Name = "Striped Long Sleeve Gray",
                        Sku = "LONG-STRIPE-002",
                        Description = "Trendy striped long sleeve with modern fit. Stylish and comfortable.",
                        Price = 220000m, SalePrice = 190000m,
                        Material = ProductMaterial.Modal,
                        Season = ProductSeason.Autumn,
                        Colors = new List<string>{ "Gray","Black","White","Navy","Blue" }
                    }
                }
            }
        };

                // Khởi tạo products + variants
                foreach (var def in seedDefinitions)
                {
                    var cat = categories.FirstOrDefault(c => c.Name == def.CategoryName);
                    if (cat == null) continue;

                    foreach (var pDef in def.Products)
                    {
                        var product = new Product
                        {
                            Id = Guid.NewGuid(),
                            Name = pDef.Name,
                            Description = pDef.Description,
                            Price = pDef.Price,
                            SalePrice = pDef.SalePrice,
                            Sku = pDef.Sku,
                            Slug = pDef.Sku.ToLowerInvariant().Replace('_', '-'),
                            CategoryId = cat.Id,
                            Status = ProductStatus.Active,
                            Material = pDef.Material,
                            Season = pDef.Season,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = adminUser.Id,
                            UpdatedBy = adminUser.Id,
                            IsDeleted = false
                        };

                        // Gắn variants
                        var variants = GenerateVariants(product, pDef.Sku, pDef.Colors,
                                                        new List<string> { "S", "M", "L", "XL", "XXL" });
                        foreach (var v in variants)
                            product.Variants.Add(v);

                        productsToSeed.Add(product);
                    }
                }

                // Thêm vào DbContext và lưu
                if (productsToSeed.Any())
                {
                    await context.Products.AddRangeAsync(productsToSeed);
                    await context.SaveChangesAsync();

                    var totalProducts = productsToSeed.Count;
                    var totalVariants = productsToSeed.Sum(p => p.Variants.Count);
                    Console.WriteLine("=== SEEDING COMPLETED ===");
                    Console.WriteLine($"Total products seeded: {totalProducts}");
                    Console.WriteLine($"Total variants seeded: {totalVariants}");
                    Console.WriteLine($"Variants per product (expected 5): {totalVariants / totalProducts}");
                }
            }
        }

        #endregion


        #region Seed Shipping Methods

        private static async Task SeedShippingMethodsAsync(T_ShirtAIcommerceContext context)
        {
            if (!await context.ShippingMethods.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var shippingMethods = new List<ShippingMethod>
        {
            new ShippingMethod
            {
                Name = ShippingCategory.Standard, // Sử dụng enum
                Description = "Vận chuyển tiêu chuẩn (3-7 ngày)",
                Fee = 20000,
                FreeShippingThreshold = 500000,
                EstimatedDays = 5,
                MinDeliveryDays = 3,
                MaxDeliveryDays = 7,
                IsActive = true,
                SortOrder = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new ShippingMethod
            {
                Name = ShippingCategory.Express,
                Description = "Vận chuyển nhanh (1-3 ngày)",
                Fee = 50000,
                FreeShippingThreshold = 1000000,
                EstimatedDays = 2,
                MinDeliveryDays = 1,
                MaxDeliveryDays = 3,
                IsActive = true,
                SortOrder = 2,
                CreatedAt = now,
                UpdatedAt = now
            },
            new ShippingMethod
            {
                Name = ShippingCategory.Overnight,
                Description = "Giao trong ngày (trước 24h)",
                Fee = 80000,
                FreeShippingThreshold = null,
                EstimatedDays = 1,
                MinDeliveryDays = 1,
                MaxDeliveryDays = 1,
                IsActive = true,
                SortOrder = 3,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

                await context.ShippingMethods.AddRangeAsync(shippingMethods);
                await context.SaveChangesAsync();
                Console.WriteLine("ShippingMethods seeded successfully");
            }
        }

        #endregion

        #region Seed Coupons

        private static async Task SeedCouponsAsync(T_ShirtAIcommerceContext context)
        {
            if (!await context.Coupons.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var coupons = new List<Coupon>
                {
                    new Coupon
                    {
                        Code = "WELCOME10",
                        Name = "Giảm 10% cho đơn đầu tiên",
                        Description = "Áp dụng cho khách hàng mới, đơn tối thiểu 200k.",
                        Type = CouponType.Percentage, // Changed to enum
                        Value = 10,
                        MinOrderAmount = 200000,
                        MaxDiscountAmount = 50000,
                        UsageLimit = 100,
                        UsageLimitPerUser = 1,
                        StartDate = now,
                        EndDate = now.AddMonths(2),
                        Status = CouponStatus.Active, // Changed to enum
                        IsFirstTimeUserOnly = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Coupon
                    {
                        Code = "SUMMER15",
                        Name = "Mùa hè giảm 15%",
                        Description = "Giảm 15% toàn bộ đơn trên 300k, tối đa 80k.",
                        Type = CouponType.Percentage, // Changed to enum
                        Value = 15,
                        MinOrderAmount = 300000,
                        MaxDiscountAmount = 80000,
                        UsageLimit = 200,
                        UsageLimitPerUser = 2,
                        StartDate = now,
                        EndDate = now.AddMonths(1),
                        Status = CouponStatus.Active, // Changed to enum
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Coupon
                    {
                        Code = "FREESHIP",
                        Name = "Miễn phí vận chuyển",
                        Description = "Miễn phí vận chuyển cho đơn từ 400k.",
                        Type = CouponType.FreeShipping, // Changed to enum
                        Value = 0,
                        MinOrderAmount = 400000,
                        MaxDiscountAmount = null,
                        UsageLimit = 500,
                        UsageLimitPerUser = 5,
                        StartDate = now,
                        EndDate = now.AddMonths(3),
                        Status = CouponStatus.Active, // Changed to enum
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Coupon
                    {
                        Code = "VIP25",
                        Name = "Khách hàng thân thiết giảm 25%",
                        Description = "Áp dụng cho khách vip, tối đa giảm 120k.",
                        Type = CouponType.Percentage, // Changed to enum
                        Value = 25,
                        MinOrderAmount = 500000,
                        MaxDiscountAmount = 120000,
                        UsageLimit = 50,
                        UsageLimitPerUser = 1,
                        StartDate = now,
                        EndDate = now.AddMonths(6),
                        Status = CouponStatus.Active, // Changed to enum
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Coupon
                    {
                        Code = "AI2025",
                        Name = "Giảm 50k đơn trên 250k",
                        Description = "Áp dụng cho tất cả, không giới hạn số lượng.",
                        Type = CouponType.FixedAmount, // Changed to enum
                        Value = 50000,
                        MinOrderAmount = 250000,
                        MaxDiscountAmount = null,
                        UsageLimit = null,
                        UsageLimitPerUser = null,
                        StartDate = now,
                        EndDate = now.AddDays(45),
                        Status = CouponStatus.Active, // Changed to enum
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                };

                await context.Coupons.AddRangeAsync(coupons);
                await context.SaveChangesAsync();
                Console.WriteLine("Coupons seeded successfully");
            }
        }

        #endregion
    }
}
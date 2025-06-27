using BusinessObjects.Common;
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
            if (!await context.Products.AnyAsync())
            {
                var categories = await context.Categories.ToListAsync();

                // Đảm bảo có admin user
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
                if (adminUser == null)
                {
                    Console.WriteLine("Admin user not found. Skipping product seeding.");
                    return;
                }

                var products = new List<Product>();

                // T-Shirt products
                var tshirtCategory = categories.FirstOrDefault(c => c.Name == "T-Shirt");
                if (tshirtCategory != null)
                {
                    products.AddRange(new[]
                    {
                        new Product
                        {
                            Name = "Basic Cotton T-Shirt White",
                            Description = "100% cotton basic t-shirt in white color. Perfect for custom designs.",
                            Price = 150000,
                            SalePrice = 120000,
                            Sku = "TSHIRT-WHITE-001",
                            Quantity = 100,
                            CategoryId = tshirtCategory.Id,
                            Material = "Cotton",
                            Season = "All Season",
                            AvailableColors = "[\"White\", \"Black\", \"Navy\", \"Gray\", \"Red\"]",
                            AvailableSizes = "[\"S\", \"M\", \"L\", \"XL\", \"XXL\"]",
                            Images = "[\"/images/products/tshirt-white-1.jpg\", \"/images/products/tshirt-white-2.jpg\"]",
                            Status = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = adminUser.Id,
                            UpdatedBy = adminUser.Id
                        },
                        new Product
                        {
                            Name = "Basic Cotton T-Shirt Black",
                            Description = "100% cotton basic t-shirt in black color. Classic and versatile.",
                            Price = 150000,
                            SalePrice = 120000,
                            Sku = "TSHIRT-BLACK-002",
                            Quantity = 100,
                            CategoryId = tshirtCategory.Id,
                            Material = "Cotton",
                            Season = "All Season",
                            AvailableColors = "[\"Black\", \"White\", \"Navy\", \"Gray\", \"Red\"]",
                            AvailableSizes = "[\"S\", \"M\", \"L\", \"XL\", \"XXL\"]",
                            Images = "[\"/images/products/tshirt-black-1.jpg\", \"/images/products/tshirt-black-2.jpg\"]",
                            Status = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = adminUser.Id,
                            UpdatedBy = adminUser.Id
                        }
                    });
                }

                // Polo products
                var poloCategory = categories.FirstOrDefault(c => c.Name == "Polo");
                if (poloCategory != null)
                {
                    products.AddRange(new[]
                    {
                        new Product
                        {
                            Name = "Classic Polo Shirt Blue",
                            Description = "Cotton blend polo shirt with ribbed collar. Professional and comfortable.",
                            Price = 220000,
                            SalePrice = 180000,
                            Sku = "POLO-BLUE-001",
                            Quantity = 80,
                            CategoryId = poloCategory.Id,
                            Material = "Cotton Blend",
                            Season = "All Season",
                            AvailableColors = "[\"Blue\", \"White\", \"Black\", \"Navy\", \"Gray\"]",
                            AvailableSizes = "[\"S\", \"M\", \"L\", \"XL\", \"XXL\"]",
                            Images = "[\"/images/products/polo-blue-1.jpg\", \"/images/products/polo-blue-2.jpg\"]",
                            Status = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = adminUser.Id,
                            UpdatedBy = adminUser.Id
                        }
                    });
                }

                // Hoodie products
                var hoodieCategory = categories.FirstOrDefault(c => c.Name == "Hoodie");
                if (hoodieCategory != null)
                {
                    products.AddRange(new[]
                    {
                        new Product
                        {
                            Name = "Premium Hoodie Gray",
                            Description = "Premium fleece hoodie with front pockets. Perfect for cold weather.",
                            Price = 350000,
                            SalePrice = 300000,
                            Sku = "HOODIE-GRAY-001",
                            Quantity = 50,
                            CategoryId = hoodieCategory.Id,
                            Material = "Fleece",
                            Season = "Winter",
                            AvailableColors = "[\"Gray\", \"Black\", \"Navy\", \"White\"]",
                            AvailableSizes = "[\"S\", \"M\", \"L\", \"XL\", \"XXL\"]",
                            Images = "[\"/images/products/hoodie-gray-1.jpg\", \"/images/products/hoodie-gray-2.jpg\"]",
                            Status = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = adminUser.Id,
                            UpdatedBy = adminUser.Id
                        }
                    });
                }

                if (products.Any())
                {
                    await context.Products.AddRangeAsync(products);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"{products.Count} products seeded successfully");
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
                        Name = "Standard",
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
                        Name = "Express",
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
                        Name = "Overnight",
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
                        Type = CouponType.Percentage,
                        Value = 10,
                        MinOrderAmount = 200000,
                        MaxDiscountAmount = 50000,
                        UsageLimit = 100,
                        UsageLimitPerUser = 1,
                        StartDate = now,
                        EndDate = now.AddMonths(2),
                        Status = CouponStatus.Active,
                        IsFirstTimeUserOnly = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Coupon
                    {
                        Code = "SUMMER15",
                        Name = "Mùa hè giảm 15%",
                        Description = "Giảm 15% toàn bộ đơn trên 300k, tối đa 80k.",
                        Type = CouponType.Percentage,
                        Value = 15,
                        MinOrderAmount = 300000,
                        MaxDiscountAmount = 80000,
                        UsageLimit = 200,
                        UsageLimitPerUser = 2,
                        StartDate = now,
                        EndDate = now.AddMonths(1),
                        Status = CouponStatus.Active,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Coupon
                    {
                        Code = "FREESHIP",
                        Name = "Miễn phí vận chuyển",
                        Description = "Miễn phí vận chuyển cho đơn từ 400k.",
                        Type = CouponType.FreeShipping,
                        Value = 0,
                        MinOrderAmount = 400000,
                        MaxDiscountAmount = null,
                        UsageLimit = 500,
                        UsageLimitPerUser = 5,
                        StartDate = now,
                        EndDate = now.AddMonths(3),
                        Status = CouponStatus.Active,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Coupon
                    {
                        Code = "VIP25",
                        Name = "Khách hàng thân thiết giảm 25%",
                        Description = "Áp dụng cho khách vip, tối đa giảm 120k.",
                        Type = CouponType.Percentage,
                        Value = 25,
                        MinOrderAmount = 500000,
                        MaxDiscountAmount = 120000,
                        UsageLimit = 50,
                        UsageLimitPerUser = 1,
                        StartDate = now,
                        EndDate = now.AddMonths(6),
                        Status = CouponStatus.Active,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Coupon
                    {
                        Code = "AI2025",
                        Name = "Giảm 50k đơn trên 250k",
                        Description = "Áp dụng cho tất cả, không giới hạn số lượng.",
                        Type = CouponType.FixedAmount,
                        Value = 50000,
                        MinOrderAmount = 250000,
                        MaxDiscountAmount = null,
                        UsageLimit = null,
                        UsageLimitPerUser = null,
                        StartDate = now,
                        EndDate = now.AddDays(45),
                        Status = CouponStatus.Active,
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
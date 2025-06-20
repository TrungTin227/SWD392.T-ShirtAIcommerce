using BusinessObjects.Common;
using BusinessObjects.Identity;
using BusinessObjects.Products;
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
                var categories = new List<Category>
                {
                    new Category
                    {
                        Name = "T-Shirt",
                        Description = "Basic cotton t-shirts for everyday wear",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Name = "Polo",
                        Description = "Polo shirts for casual and semi-formal occasions",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Name = "Hoodie",
                        Description = "Hooded sweatshirts for cold weather",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Name = "Tank Top",
                        Description = "Sleeveless shirts for summer",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Name = "Long Sleeve",
                        Description = "Long-sleeved shirts for cooler weather",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
                Console.WriteLine("Categories seeded successfully");
            }
        }

        #endregion
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
                var tshirtCategory = categories.First(c => c.Name == "T-Shirt");
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
                CreatedBy = adminUser.Id,  // Sử dụng ID thực tế
                UpdatedBy = adminUser.Id
            },
            // ... các products khác
        });

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
                Console.WriteLine($"{products.Count} products seeded successfully");
            }
        }
    }
}
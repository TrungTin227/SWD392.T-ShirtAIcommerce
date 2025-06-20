using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Repositories
{
    public class T_ShirtAIcommerceContextFactory : IDesignTimeDbContextFactory<T_ShirtAIcommerceContext>
    {
        public T_ShirtAIcommerceContext CreateDbContext(string[] args)
        {
            // 1. Tìm đường dẫn đến project WebAPI (chứa appsettings.json)
            var basePath = FindWebApiProjectPath();

            // 2. Build configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets("1b791acd-7051-4c9c-93d3-be084c7bc7e5")
                .AddEnvironmentVariables()
                .Build();

            // 3. Lấy connection string
            var connectionString = config.GetConnectionString("T_ShirtAIcommerceContext");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'T_ShirtAIcommerceContext' not found.");
            }

            Console.WriteLine($"Using connection: {connectionString}");

            // 4. Build DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<T_ShirtAIcommerceContext>();
            optionsBuilder.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly("Repositories")
            );

            // 5. ✅ Pass null - DbContext sẽ handle null HttpContextAccessor
            return new T_ShirtAIcommerceContext(optionsBuilder.Options, null!);
        }

        private string FindWebApiProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDirectory);

            // Tìm thư mục chứa appsettings.json (thường là WebAPI project)
            while (directory != null)
            {
                var appsettingsPath = Path.Combine(directory.FullName, "appsettings.json");
                if (File.Exists(appsettingsPath))
                {
                    return directory.FullName;
                }

                // Tìm trong các thư mục con
                var webApiDir = directory.GetDirectories()
                    .FirstOrDefault(d => d.Name.Contains("WebAPI") || d.Name.Contains("API"));

                if (webApiDir != null)
                {
                    var webApiAppsettings = Path.Combine(webApiDir.FullName, "appsettings.json");
                    if (File.Exists(webApiAppsettings))
                    {
                        return webApiDir.FullName;
                    }
                }

                directory = directory.Parent;
            }

            // Fallback về current directory
            return currentDirectory;
        }
    }
}
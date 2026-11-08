using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OrderInventory.Api.Data;
using OrderInventory.Api.Identity;
using OrderInventory.Api.Models;

namespace OrderInventory.Tests.IntegrationTests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsATestSecretKeyForIntegrationTestsOnly123456",
                ["Jwt:Issuer"] = "OrderInventory.Tests",
                ["Jwt:Audience"] = "OrderInventory.Tests",
                ["RateLimiting:AuthPermitLimit"] = "1000",
                ["RateLimiting:GeneralPermitLimit"] = "1000"
            };

            config.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices((context, services) =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            var connectionString = context.Configuration.GetConnectionString("TestConnection")
                ?? "Server=localhost;Database=OrderInventoryIntegrationTestDb;Trusted_Connection=True;TrustServerCertificate=True;";

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
        });
    }

    private static readonly object _dbInitLock = new();
    private static bool _dbInitialized = false;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();

        lock (_dbInitLock)
        {
            if (!_dbInitialized)
            {
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;

                var dbContext =
                    services.GetRequiredService<AppDbContext>();

                // Apply database migrations
                dbContext.Database.Migrate();

                var userManager =
                    services.GetRequiredService<UserManager<ApplicationUser>>();

                var roleManager =
                    services.GetRequiredService<RoleManager<IdentityRole>>();

                // Seed test Admin user and role
                SeedTestUserAsync(userManager, roleManager)
                    .GetAwaiter()
                    .GetResult();

                _dbInitialized = true;
            }
        }

        host.Start();

        return host;
    }

    private static async Task SeedTestUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        const string email = "integration-admin@example.com";
        const string password = "Test1234";
        const string roleName = "Admin";

        // 1. Ensure Admin role exists
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole(roleName));

            if (!roleResult.Succeeded)
            {
                throw new Exception(
                    string.Join(
                        ", ",
                        roleResult.Errors.Select(e => e.Description)));
            }
        }

        // 2. Find the test user
        var user = await userManager.FindByEmailAsync(email);

        // 3. Create the user if it doesn't exist
        if (user == null)
        {
            user = new ApplicationUser
            {
                FullName = "Integration Admin",
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(
                user,
                password);

            if (!result.Succeeded)
            {
                throw new Exception(
                    string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description)));
            }
        }

        // 4. Ensure the user belongs to Admin role
        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var result = await userManager.AddToRoleAsync(
                user,
                roleName);

            if (!result.Succeeded)
            {
                throw new Exception(
                    string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description)));
            }
        }
    }
}

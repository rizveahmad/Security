using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Security.Infrastructure.Identity;

namespace Security.Infrastructure.Data;

/// <summary>
/// Provides idempotent startup seeding for ASP.NET Core Identity data
/// (default roles and the Super Admin user).
///
/// Call from the application startup after the hosted services have run
/// so that the Identity schema and business tables already exist.
/// Provides a clean seam for initial database seed logic (e.g. Super Admin bootstrap).
/// Call from the application startup / Program.cs after migrations have been applied.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // EnsureCreated creates the Identity schema when no EF migrations exist.
            // This is safe to call on every startup; it is a no-op when tables already exist.
            await context.Database.EnsureCreatedAsync();

            await SeedRolesAsync(scope.ServiceProvider, logger);
            await SeedSuperAdminAsync(scope.ServiceProvider, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(
        IServiceProvider services,
        ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = ["SuperAdmin", "Admin", "User"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private static async Task SeedSuperAdminAsync(
        IServiceProvider services,
        ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        const string superAdminEmail = "superadmin@security.local";

        if (await userManager.FindByEmailAsync(superAdminEmail) is null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                IsActive = true,
                CreatedBy = "System",
                CreatedDate = DateTime.UtcNow,
            };

            // Password is read from configuration (Seed:SuperAdminPassword).
            // Set this via user-secrets, environment variables, or Azure Key Vault – never hard-code in source.
            var password = configuration["Seed:SuperAdminPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Skipping Super Admin seed because Seed:SuperAdminPassword is not configured.");
                return;
            }

            var result = await userManager.CreateAsync(superAdmin, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                logger.LogInformation("Super Admin user created");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create Super Admin: {Errors}", errors);
            }
        }
    }
}

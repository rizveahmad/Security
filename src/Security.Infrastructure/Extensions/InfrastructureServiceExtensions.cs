using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Security.Application.Authorization;
using Security.Application.Common.Interfaces;
using Security.Infrastructure.Authorization;
using Security.Infrastructure.Data;
using Security.Infrastructure.Identity;

namespace Security.Infrastructure.Extensions;

/// <summary>
/// Registers all Infrastructure-layer services (EF Core, Identity, SqlScriptRunner, etc.)
/// into the DI container.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Application services
        services.AddScoped<IUserCreationService, UserCreationService>();
        services.AddScoped<IUserQueryService, UserQueryService>();

        // Dynamic permission authorization
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IAuthorizationHandler, DynamicPermissionHandler>();
        // Hosted service that runs pending numbered SQL scripts on startup.
        services.AddHostedService<SqlScriptRunner>();

        return services;
    }
}

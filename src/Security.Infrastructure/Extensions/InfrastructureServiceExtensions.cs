using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Security.Application.Authorization;
using Security.Application.Common.Interfaces;
using Security.Application.Interfaces;
using Security.Infrastructure.Authorization;
using Security.Infrastructure.Data;
using Security.Infrastructure.Identity;
using Security.Infrastructure.Services;

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
                // Password policy (min 10 chars; upper + lower + digit + symbol required).
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                // Account lockout: 5 failed attempts → 15-minute lockout for all users.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<CommonPasswordValidator>();

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        // Application services
        services.AddScoped<IUserCreationService, UserCreationService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped(typeof(IExportService<>), typeof(ExcelExportService<>));
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddHttpContextAccessor();

        // Dynamic permission authorization
        services.AddMemoryCache();
        services.AddSingleton<IPermissionCache, InMemoryPermissionCache>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IAuthorizationHandler, DynamicPermissionHandler>();

        // JWT token issuance
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Database bootstrap: script runner + deterministic startup initializer.
        services.AddScoped<IDatabaseScriptRunner, SqlScriptRunner>();
        services.AddScoped<IDatabaseBootstrapper, DatabaseBootstrapper>();

        return services;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Security.Application.Authorization;

namespace Security.Infrastructure.Authorization;

/// <summary>
/// Handles PermissionRequirement by checking the DB-backed permission service.
/// SuperAdmin identity role bypasses all permission checks.
/// </summary>
public class DynamicPermissionHandler(IServiceScopeFactory scopeFactory) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
            return;

        // SuperAdmin bypasses permission checks
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return;

        using var scope = scopeFactory.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        if (await permissionService.HasPermissionAsync(userId, requirement.PermissionCode))
            context.Succeed(requirement);
    }
}

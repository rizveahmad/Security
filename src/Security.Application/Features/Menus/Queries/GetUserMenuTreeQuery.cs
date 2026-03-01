using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Authorization;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Menus.Queries;

/// <summary>
/// Returns the menu tree visible to a specific user within the active tenant.
/// Only menus whose permission types appear in the user's permission set are included.
/// </summary>
public record GetUserMenuTreeQuery(string UserId) : IRequest<IReadOnlyList<MenuModuleDto>>;

/// <summary>Module node in the menu tree.</summary>
public record MenuModuleDto(int ModuleId, string ModuleName, IReadOnlyList<MenuItemDto> Menus);

/// <summary>Menu leaf in the menu tree.</summary>
public record MenuItemDto(int Id, string Name, string? Code, string? Url, string? Icon, int DisplayOrder);

public class GetUserMenuTreeQueryHandler(
    IApplicationDbContext context,
    IPermissionService permissionService)
    : IRequestHandler<GetUserMenuTreeQuery, IReadOnlyList<MenuModuleDto>>
{
    public async Task<IReadOnlyList<MenuModuleDto>> Handle(GetUserMenuTreeQuery request, CancellationToken ct)
    {
        // Fetch all permission codes assigned to this user (cached by IPermissionService).
        var userPermissions = await permissionService.GetUserPermissionsAsync(request.UserId, ct);

        // Fetch all active menus together with their module and permission types.
        var menus = await context.AppMenus
            .AsNoTracking()
            .Include(m => m.Module)
            .Include(m => m.PermissionTypes)
            .Where(m => m.IsActive)
            .ToListAsync(ct);

        // Keep only menus where the user holds at least one linked permission.
        var accessible = menus
            .Where(m =>
                m.PermissionTypes.Count == 0 ||  // menus with no permission gate are visible to all
                m.PermissionTypes.Any(pt => pt.Code != null && userPermissions.Contains(pt.Code, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        // Group by module to form the tree.
        var tree = accessible
            .Where(m => m.Module != null)
            .GroupBy(m => m.ModuleId)
            .OrderBy(g => g.First().Module!.Name)
            .Select(g => new MenuModuleDto(
                g.Key,
                g.First().Module!.Name,
                g.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
                 .Select(m => new MenuItemDto(m.Id, m.Name, m.Code, m.Url, m.Icon, m.DisplayOrder))
                 .ToList()))
            .ToList();

        return tree;
    }
}

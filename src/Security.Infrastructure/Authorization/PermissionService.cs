using Microsoft.EntityFrameworkCore;
using Security.Application.Authorization;
using Security.Infrastructure.Data;

namespace Security.Infrastructure.Authorization;

/// <summary>
/// DB-backed permission evaluation: user -> role group -> roles -> permission types.
/// </summary>
public class PermissionService(ApplicationDbContext context) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(string userId, string permissionCode, CancellationToken ct = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, ct);
        return permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(string userId, CancellationToken ct = default)
    {
        var permissions = await (
            from urg in context.UserRoleGroups.AsNoTracking()
            where urg.UserId == userId && !urg.IsDeleted
            join rgr in context.RoleGroupRoles.AsNoTracking() on urg.RoleGroupId equals rgr.RoleGroupId
            where !rgr.IsDeleted
            join rp in context.RolePermissions.AsNoTracking() on rgr.RoleId equals rp.RoleId
            where !rp.IsDeleted
            join pt in context.PermissionTypes.AsNoTracking() on rp.PermissionTypeId equals pt.Id
            where pt.Code != null
            select pt.Code
        ).Where(c => c != null).Distinct().ToListAsync(ct);

        return permissions!;
    }
}

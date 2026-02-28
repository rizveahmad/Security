using Microsoft.EntityFrameworkCore;
using Security.Domain.Common;
using Security.Domain.Entities;

namespace Security.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the application's write-side persistence context.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<AppModule> AppModules { get; }
    DbSet<AppMenu> AppMenus { get; }
    DbSet<PermissionType> PermissionTypes { get; }
    DbSet<AppRole> AppRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RoleGroup> RoleGroups { get; }
    DbSet<RoleGroupRole> RoleGroupRoles { get; }
    DbSet<UserRoleGroup> UserRoleGroups { get; }
    DbSet<Workstation> Workstations { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

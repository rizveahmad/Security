using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Domain.Common;
using Security.Domain.Entities;
using Security.Infrastructure.Identity;

namespace Security.Infrastructure.Data;

/// <summary>
/// Main EF Core database context wiring Identity and application entities.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<AppModule> AppModules => Set<AppModule>();
    public DbSet<AppMenu> AppMenus => Set<AppMenu>();
    public DbSet<PermissionType> PermissionTypes => Set<PermissionType>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleGroup> RoleGroups => Set<RoleGroup>();
    public DbSet<RoleGroupRole> RoleGroupRoles => Set<RoleGroupRole>();
    public DbSet<UserRoleGroup> UserRoleGroups => Set<UserRoleGroup>();
    public DbSet<Workstation> Workstations => Set<Workstation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global soft-delete query filters for all AuditableEntity-derived types
        builder.Entity<Company>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AppModule>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AppMenu>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PermissionType>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AppRole>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<RolePermission>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<RoleGroup>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<RoleGroupRole>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<UserRoleGroup>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Workstation>().HasQueryFilter(e => !e.IsDeleted);
    }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Interfaces;
using Security.Domain.Common;
using Security.Domain.Entities;
using ApplicationUser = Security.Infrastructure.Identity.ApplicationUser;

namespace Security.Infrastructure.Data;

/// <summary>
/// Main EF Core database context wiring Identity and application entities.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext? tenantContext = null)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    /// <summary>
    /// Active tenant identifier evaluated per-instance at query time.
    /// Null means no tenant filter is applied (SuperAdmin viewing all tenants).
    /// </summary>
    private int? ActiveTenantId => tenantContext?.TenantId;

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

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.EntityName).HasMaxLength(100);
            e.Property(a => a.EntityId).HasMaxLength(100);
            e.Property(a => a.UserName).HasMaxLength(256);
            e.Property(a => a.IpAddress).HasMaxLength(50);
        });
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global soft-delete query filters for all AuditableEntity-derived types.
        // Tenant-scoped entities (those with a direct CompanyId) additionally enforce
        // tenant isolation: records are only visible when their CompanyId matches the
        // active tenant, unless no tenant is selected (ActiveTenantId == null) which
        // allows SuperAdmin to view records across all tenants.
        builder.Entity<Company>().HasQueryFilter(e => e.DeletedDate == null);
        builder.Entity<AppModule>().HasQueryFilter(e => e.DeletedDate == null
            && (ActiveTenantId == null || e.CompanyId == ActiveTenantId));
        builder.Entity<AppMenu>().HasQueryFilter(e => e.DeletedDate == null);
        builder.Entity<PermissionType>().HasQueryFilter(e => e.DeletedDate == null);
        builder.Entity<AppRole>().HasQueryFilter(e => e.DeletedDate == null
            && (ActiveTenantId == null || e.CompanyId == ActiveTenantId));
        builder.Entity<RolePermission>().HasQueryFilter(e => e.DeletedDate == null);
        builder.Entity<RoleGroup>().HasQueryFilter(e => e.DeletedDate == null
            && (ActiveTenantId == null || e.CompanyId == ActiveTenantId));
        builder.Entity<RoleGroupRole>().HasQueryFilter(e => e.DeletedDate == null);
        builder.Entity<UserRoleGroup>().HasQueryFilter(e => e.DeletedDate == null);
        builder.Entity<Workstation>().HasQueryFilter(e => e.DeletedDate == null
            && (ActiveTenantId == null || e.CompanyId == ActiveTenantId));
    }
}

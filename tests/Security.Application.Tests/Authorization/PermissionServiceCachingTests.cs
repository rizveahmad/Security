using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Security.Application.Authorization;
using Security.Domain.Entities;
using Security.Infrastructure.Authorization;
using Security.Infrastructure.Data;

namespace Security.Application.Tests.Authorization;

/// <summary>
/// Integration-style unit tests for <see cref="PermissionService"/> verifying
/// cache-aside behaviour and cache invalidation without a live SQL Server instance.
/// Uses an in-process SQLite database; ApplicationDbContext is created with
/// null ITenantContext so global tenant query filters are bypassed (SuperAdmin mode).
/// </summary>
public class PermissionServiceCachingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IMemoryCache _memoryCache;
    private readonly InMemoryPermissionCache _permissionCache;
    private readonly ApplicationDbContext _db;

    public PermissionServiceCachingTests()
    {
        // Keep the SQLite connection open for the test lifetime so the in-memory
        // database persists across EnsureCreated and query calls.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _permissionCache = new InMemoryPermissionCache(_memoryCache);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        // null tenantContext → ActiveTenantId = null → no tenant filter applied.
        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Dispose();
        _memoryCache.Dispose();
        _connection.Dispose();
    }

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

    private const string UserId = "test-user-1";
    private const int TenantId = 1;
    private const string PermCode = "products.read";

    private void SeedTestData()
    {
        // Company must exist first because AppRole, RoleGroup, and AppModule have a CompanyId FK.
        var company = new Company
        {
            Name = "Test Company",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _db.Companies.Add(company);
        _db.SaveChanges();

        // AppModule → AppMenu → PermissionType chain required by FK constraints.
        var module = new AppModule
        {
            Name = "Test Module",
            CompanyId = company.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _db.AppModules.Add(module);
        _db.SaveChanges();

        var menu = new AppMenu
        {
            Name = "Test Menu",
            ModuleId = module.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _db.AppMenus.Add(menu);
        _db.SaveChanges();

        var permType = new PermissionType
        {
            Code = PermCode,
            Name = "Products Read",
            MenuId = menu.Id,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _db.PermissionTypes.Add(permType);

        var role = new AppRole
        {
            Name = "Viewer",
            Code = "viewer",
            CompanyId = company.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _db.AppRoles.Add(role);

        var group = new RoleGroup
        {
            Name = "ViewerGroup",
            CompanyId = company.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _db.RoleGroups.Add(group);

        _db.SaveChanges();

        _db.RoleGroupRoles.Add(new RoleGroupRole
        {
            RoleGroupId = group.Id,
            RoleId = role.Id,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        _db.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionTypeId = permType.Id,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        _db.UserRoleGroups.Add(new UserRoleGroup
        {
            UserId = UserId,
            RoleGroupId = group.Id,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        _db.SaveChanges();
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsMappedPermissions()
    {
        var svc = new PermissionService(_db, _permissionCache, new FakeTenantContext(TenantId));

        var permissions = await svc.GetUserPermissionsAsync(UserId);

        Assert.Contains(PermCode, permissions, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_PopulatesCache_OnFirstCall()
    {
        var svc = new PermissionService(_db, _permissionCache, new FakeTenantContext(TenantId));

        // Cache is empty before the call.
        Assert.Null(_permissionCache.Get(TenantId, UserId));

        await svc.GetUserPermissionsAsync(UserId);

        // Cache is populated after the first call.
        var cached = _permissionCache.Get(TenantId, UserId);
        Assert.NotNull(cached);
        Assert.Contains(PermCode, cached, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsCachedResult_OnSecondCall()
    {
        var svc = new PermissionService(_db, _permissionCache, new FakeTenantContext(TenantId));

        var first = await svc.GetUserPermissionsAsync(UserId);

        // Pre-populate cache with a known-different set.
        _permissionCache.Set(TenantId, UserId, ["overridden.perm"]);

        var second = await svc.GetUserPermissionsAsync(UserId);

        Assert.Contains("overridden.perm", second, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(PermCode, second, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_AfterInvalidateUser_RecalculatesFromDb()
    {
        var svc = new PermissionService(_db, _permissionCache, new FakeTenantContext(TenantId));

        // Warm up cache.
        await svc.GetUserPermissionsAsync(UserId);
        Assert.NotNull(_permissionCache.Get(TenantId, UserId));

        // Invalidate and recalculate.
        _permissionCache.InvalidateUser(TenantId, UserId);
        Assert.Null(_permissionCache.Get(TenantId, UserId));

        var result = await svc.GetUserPermissionsAsync(UserId);

        Assert.Contains(PermCode, result, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_AfterInvalidateTenant_RecalculatesFromDb()
    {
        var svc = new PermissionService(_db, _permissionCache, new FakeTenantContext(TenantId));

        await svc.GetUserPermissionsAsync(UserId);
        Assert.NotNull(_permissionCache.Get(TenantId, UserId));

        _permissionCache.InvalidateTenant(TenantId);
        Assert.Null(_permissionCache.Get(TenantId, UserId));

        var result = await svc.GetUserPermissionsAsync(UserId);

        Assert.Contains(PermCode, result, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HasPermissionAsync_ReturnsFalse_ForUnknownPermission()
    {
        var svc = new PermissionService(_db, _permissionCache, new FakeTenantContext(TenantId));

        var result = await svc.HasPermissionAsync(UserId, "nonexistent.permission");

        Assert.False(result);
    }

    [Fact]
    public async Task HasPermissionAsync_ReturnsTrue_ForKnownPermission()
    {
        var svc = new PermissionService(_db, _permissionCache, new FakeTenantContext(TenantId));

        var result = await svc.HasPermissionAsync(UserId, PermCode);

        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // Fake collaborators
    // -----------------------------------------------------------------------

    private sealed class FakeTenantContext(int? tenantId) : Security.Application.Interfaces.ITenantContext
    {
        public int? TenantId { get; } = tenantId;
        public bool IsSuperAdmin => false;
    }
}

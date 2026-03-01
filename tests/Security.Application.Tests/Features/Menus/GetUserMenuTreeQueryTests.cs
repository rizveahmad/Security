using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Security.Application.Authorization;
using Security.Application.Features.Menus.Queries;
using Security.Domain.Entities;
using Security.Infrastructure.Data;

namespace Security.Application.Tests.Features.Menus;

/// <summary>
/// Unit tests for <see cref="GetUserMenuTreeQueryHandler"/> verifying
/// that the returned tree respects the user's permission set and active-menu filter.
/// xUnit creates a new class instance per test, so each test gets its own fresh
/// in-process SQLite database via the constructor (same pattern as PermissionServiceCachingTests).
/// The permission service is faked to decouple handler logic from permission resolution.
/// </summary>
public class GetUserMenuTreeQueryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;

    public GetUserMenuTreeQueryTests()
    {
        // Use a unique named shared-cache database per class instance.
        // This ensures the database is truly isolated even when the connection pool
        // reuses physical connections — each GUID name maps to a distinct in-memory store.
        var dbName = $"testdb-{Guid.NewGuid():N}";
        _connection = new SqliteConnection($"DataSource={dbName};Mode=Memory;Cache=Shared");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        // null tenantContext → ActiveTenantId == null → no tenant filter applied (SuperAdmin mode).
        _db = new ApplicationDbContext(options, tenantContext: null);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

    private Company SeedCompany()
    {
        var company = new Company
        {
            Name = "Test Co",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _db.Companies.Add(company);
        _db.SaveChanges();
        return company;
    }

    private AppModule SeedModule(int companyId, string name = "Module A")
    {
        var module = new AppModule
        {
            Name = name,
            CompanyId = companyId,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _db.AppModules.Add(module);
        _db.SaveChanges();
        return module;
    }

    private AppMenu SeedMenu(int moduleId, string name,
        bool isActive = true, string? permCode = null, int displayOrder = 0)
    {
        var menu = new AppMenu
        {
            Name = name,
            ModuleId = moduleId,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _db.AppMenus.Add(menu);
        _db.SaveChanges();

        if (permCode != null)
        {
            _db.PermissionTypes.Add(new PermissionType
            {
                Code = permCode,
                Name = permCode,
                MenuId = menu.Id,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "seed"
            });
            _db.SaveChanges();
        }

        return menu;
    }

    private GetUserMenuTreeQueryHandler CreateHandler(IReadOnlyList<string> userPermissions)
        => new(_db, new FakePermissionService(userPermissions));

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Handle_ReturnsAccessibleMenus_ForUserWithPermission()
    {
        var company = SeedCompany();
        var module = SeedModule(company.Id);
        SeedMenu(module.Id, "Menu A", permCode: "menu.a.view");

        var result = await CreateHandler(["menu.a.view"])
            .Handle(new GetUserMenuTreeQuery("user-1"), default);

        Assert.Single(result);
        Assert.Equal("Module A", result[0].ModuleName);
        Assert.Single(result[0].Menus);
        Assert.Equal("Menu A", result[0].Menus[0].Name);
    }

    [Fact]
    public async Task Handle_ExcludesMenus_WhenUserLacksPermission()
    {
        var company = SeedCompany();
        var module = SeedModule(company.Id);
        SeedMenu(module.Id, "Menu A", permCode: "menu.a.view");

        var result = await CreateHandler([]) // no permissions
            .Handle(new GetUserMenuTreeQuery("user-1"), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_IncludesMenus_WithNoPermissionGate()
    {
        var company = SeedCompany();
        var module = SeedModule(company.Id);
        SeedMenu(module.Id, "Open Menu"); // no permission code → visible to all

        var result = await CreateHandler([]) // no permissions
            .Handle(new GetUserMenuTreeQuery("user-1"), default);

        Assert.Single(result);
        Assert.Single(result[0].Menus);
        Assert.Equal("Open Menu", result[0].Menus[0].Name);
    }

    [Fact]
    public async Task Handle_ExcludesInactiveMenus()
    {
        var company = SeedCompany();
        var module = SeedModule(company.Id);
        SeedMenu(module.Id, "Active Menu", isActive: true);
        SeedMenu(module.Id, "Inactive Menu", isActive: false);

        var result = await CreateHandler([])
            .Handle(new GetUserMenuTreeQuery("user-1"), default);

        var menus = result.SelectMany(m => m.Menus).ToList();
        Assert.Single(menus);
        Assert.Equal("Active Menu", menus[0].Name);
    }

    [Fact]
    public async Task Handle_GroupsMenusByModule()
    {
        var company = SeedCompany();
        var moduleA = SeedModule(company.Id, "Module A");
        var moduleB = SeedModule(company.Id, "Module B");
        SeedMenu(moduleA.Id, "Menu A1");
        SeedMenu(moduleA.Id, "Menu A2");
        SeedMenu(moduleB.Id, "Menu B1");

        var result = await CreateHandler([])
            .Handle(new GetUserMenuTreeQuery("user-1"), default);

        Assert.Equal(2, result.Count);
        var modA = result.First(r => r.ModuleName == "Module A");
        Assert.Equal(2, modA.Menus.Count);
        var modB = result.First(r => r.ModuleName == "Module B");
        Assert.Single(modB.Menus);
    }

    [Fact]
    public async Task Handle_ReturnsMenusSortedByDisplayOrder()
    {
        var company = SeedCompany();
        var module = SeedModule(company.Id);
        SeedMenu(module.Id, "Second", displayOrder: 2);
        SeedMenu(module.Id, "First", displayOrder: 1);

        var result = await CreateHandler([])
            .Handle(new GetUserMenuTreeQuery("user-1"), default);

        var menus = result[0].Menus;
        Assert.Equal("First", menus[0].Name);
        Assert.Equal("Second", menus[1].Name);
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoMenusExist()
    {
        var result = await CreateHandler(["any.permission"])
            .Handle(new GetUserMenuTreeQuery("user-1"), default);

        Assert.Empty(result);
    }

    // -----------------------------------------------------------------------
    // Fake collaborators
    // -----------------------------------------------------------------------

    private sealed class FakePermissionService(IReadOnlyList<string> permissions) : IPermissionService
    {
        public Task<bool> HasPermissionAsync(string userId, string permissionCode, CancellationToken ct = default)
            => Task.FromResult(permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase));

        public Task<IReadOnlyList<string>> GetUserPermissionsAsync(string userId, CancellationToken ct = default)
            => Task.FromResult(permissions);
    }
}

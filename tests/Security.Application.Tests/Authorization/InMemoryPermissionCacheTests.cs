using Microsoft.Extensions.Caching.Memory;
using Security.Infrastructure.Authorization;

namespace Security.Application.Tests.Authorization;

/// <summary>
/// Unit tests for InMemoryPermissionCache verifying cache hit/miss semantics
/// and tenant-aware invalidation without requiring a live database.
/// </summary>
public class InMemoryPermissionCacheTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly InMemoryPermissionCache _cache;

    public InMemoryPermissionCacheTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cache = new InMemoryPermissionCache(_memoryCache);
    }

    public void Dispose() => _memoryCache.Dispose();

    [Fact]
    public void Get_ReturnsNull_WhenNotCached()
    {
        var result = _cache.Get(tenantId: 1, userId: "user1");

        Assert.Null(result);
    }

    [Fact]
    public void Set_Then_Get_ReturnsCachedPermissions()
    {
        var permissions = new List<string> { "perm.read", "perm.write" };

        _cache.Set(tenantId: 1, userId: "user1", permissions);
        var result = _cache.Get(tenantId: 1, userId: "user1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("perm.read", result);
        Assert.Contains("perm.write", result);
    }

    [Fact]
    public void CacheEntries_AreIsolatedByTenant()
    {
        var perms1 = new List<string> { "tenant1.perm" };
        var perms2 = new List<string> { "tenant2.perm" };

        _cache.Set(tenantId: 1, userId: "user1", perms1);
        _cache.Set(tenantId: 2, userId: "user1", perms2);

        var result1 = _cache.Get(tenantId: 1, userId: "user1");
        var result2 = _cache.Get(tenantId: 2, userId: "user1");

        Assert.NotNull(result1);
        Assert.Contains("tenant1.perm", result1);
        Assert.NotNull(result2);
        Assert.Contains("tenant2.perm", result2);
    }

    [Fact]
    public void CacheEntries_AreIsolatedByUser()
    {
        var permsA = new List<string> { "userA.perm" };
        var permsB = new List<string> { "userB.perm" };

        _cache.Set(tenantId: 1, userId: "userA", permsA);
        _cache.Set(tenantId: 1, userId: "userB", permsB);

        var resultA = _cache.Get(tenantId: 1, userId: "userA");
        var resultB = _cache.Get(tenantId: 1, userId: "userB");

        Assert.NotNull(resultA);
        Assert.Contains("userA.perm", resultA);
        Assert.NotNull(resultB);
        Assert.Contains("userB.perm", resultB);
    }

    [Fact]
    public void InvalidateUser_RemovesOnlyThatUsersEntry()
    {
        _cache.Set(tenantId: 1, userId: "user1", ["perm.a"]);
        _cache.Set(tenantId: 1, userId: "user2", ["perm.b"]);

        _cache.InvalidateUser(tenantId: 1, userId: "user1");

        Assert.Null(_cache.Get(tenantId: 1, userId: "user1"));
        Assert.NotNull(_cache.Get(tenantId: 1, userId: "user2"));
    }

    [Fact]
    public void InvalidateTenant_RemovesAllUsersInTenant()
    {
        _cache.Set(tenantId: 1, userId: "user1", ["perm.a"]);
        _cache.Set(tenantId: 1, userId: "user2", ["perm.b"]);
        _cache.Set(tenantId: 2, userId: "user1", ["perm.c"]);

        _cache.InvalidateTenant(tenantId: 1);

        Assert.Null(_cache.Get(tenantId: 1, userId: "user1"));
        Assert.Null(_cache.Get(tenantId: 1, userId: "user2"));
        // tenant 2 is unaffected
        Assert.NotNull(_cache.Get(tenantId: 2, userId: "user1"));
    }

    [Fact]
    public void InvalidateTenant_WhenNoneSet_DoesNotThrow()
    {
        // Should be a no-op
        _cache.InvalidateTenant(tenantId: 99);
    }

    [Fact]
    public void InvalidateAll_RemovesAllEntries()
    {
        _cache.Set(tenantId: 1, userId: "user1", ["perm.a"]);
        _cache.Set(tenantId: 2, userId: "user2", ["perm.b"]);
        _cache.Set(null, userId: "superadmin", ["perm.c"]);

        _cache.InvalidateAll();

        Assert.Null(_cache.Get(tenantId: 1, userId: "user1"));
        Assert.Null(_cache.Get(tenantId: 2, userId: "user2"));
        Assert.Null(_cache.Get(null, userId: "superadmin"));
    }

    [Fact]
    public void Set_AfterInvalidateTenant_WorksCorrectly()
    {
        _cache.Set(tenantId: 1, userId: "user1", ["old.perm"]);
        _cache.InvalidateTenant(tenantId: 1);

        _cache.Set(tenantId: 1, userId: "user1", ["new.perm"]);
        var result = _cache.Get(tenantId: 1, userId: "user1");

        Assert.NotNull(result);
        Assert.Contains("new.perm", result);
    }

    [Fact]
    public void NullTenantId_IsCachedSeparatelyFromNonNull()
    {
        _cache.Set(null, userId: "user1", ["global.perm"]);
        _cache.Set(tenantId: 1, userId: "user1", ["tenant.perm"]);

        var global = _cache.Get(null, userId: "user1");
        var tenant = _cache.Get(tenantId: 1, userId: "user1");

        Assert.NotNull(global);
        Assert.Contains("global.perm", global);
        Assert.NotNull(tenant);
        Assert.Contains("tenant.perm", tenant);
    }
}

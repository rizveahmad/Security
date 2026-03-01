using Microsoft.AspNetCore.Http;
using Security.Infrastructure.Services;
using System.Security.Claims;

namespace Security.Application.Tests.Infrastructure;

/// <summary>
/// Unit tests for TenantContext verifying resolution order (JWT claim → header → session)
/// and SuperAdmin bypass behavior without requiring a live HTTP server.
/// </summary>
public class TenantContextTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a minimal DefaultHttpContext with the specified claim, header, and session values.
    /// </summary>
    private static IHttpContextAccessor BuildAccessor(
        string? tidClaim = null,
        string? tenantHeader = null,
        int? sessionTenant = null,
        bool isSuperAdmin = false)
    {
        var ctx = new DefaultHttpContext();

        // Set up claims principal
        var claims = new List<Claim>();
        if (tidClaim != null)
            claims.Add(new Claim("tid", tidClaim));
        if (isSuperAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        // Set up request header
        if (tenantHeader != null)
            ctx.Request.Headers["X-Tenant-Id"] = tenantHeader;

        // Always attach a session so ctx.Session does not throw
        var session = new FakeSession();
        if (sessionTenant.HasValue)
            session.SetInt32("SelectedTenantId", sessionTenant.Value);
        ctx.Session = session;

        var accessor = new FakeHttpContextAccessor(ctx);
        return accessor;
    }

    // -----------------------------------------------------------------------
    // TenantId resolution tests
    // -----------------------------------------------------------------------

    [Fact]
    public void TenantId_ReturnsNull_WhenNoSourceSet()
    {
        var sut = new TenantContext(BuildAccessor());

        Assert.Null(sut.TenantId);
    }

    [Fact]
    public void TenantId_ReturnsFromClaim_WhenTidClaimPresent()
    {
        var sut = new TenantContext(BuildAccessor(tidClaim: "7"));

        Assert.Equal(7, sut.TenantId);
    }

    [Fact]
    public void TenantId_ReturnsFromHeader_WhenNoClaimButHeaderSet()
    {
        var sut = new TenantContext(BuildAccessor(tenantHeader: "42"));

        Assert.Equal(42, sut.TenantId);
    }

    [Fact]
    public void TenantId_ReturnsFromSession_WhenNoClaimOrHeader()
    {
        var sut = new TenantContext(BuildAccessor(sessionTenant: 99));

        Assert.Equal(99, sut.TenantId);
    }

    [Fact]
    public void TenantId_ClaimTakesPrecedenceOverHeader()
    {
        // Claim "tid" = 1, header "X-Tenant-Id" = 2 → claim wins
        var sut = new TenantContext(BuildAccessor(tidClaim: "1", tenantHeader: "2"));

        Assert.Equal(1, sut.TenantId);
    }

    [Fact]
    public void TenantId_HeaderTakesPrecedenceOverSession()
    {
        // Header = 5, session = 10 → header wins
        var sut = new TenantContext(BuildAccessor(tenantHeader: "5", sessionTenant: 10));

        Assert.Equal(5, sut.TenantId);
    }

    [Fact]
    public void TenantId_ClaimTakesPrecedenceOverHeaderAndSession()
    {
        var sut = new TenantContext(BuildAccessor(tidClaim: "3", tenantHeader: "5", sessionTenant: 7));

        Assert.Equal(3, sut.TenantId);
    }

    [Fact]
    public void TenantId_ReturnsNull_WhenHttpContextIsNull()
    {
        var accessor = new FakeHttpContextAccessor(null);
        var sut = new TenantContext(accessor);

        Assert.Null(sut.TenantId);
    }

    [Fact]
    public void TenantId_ReturnsNull_WhenTidClaimIsNotAnInteger()
    {
        var sut = new TenantContext(BuildAccessor(tidClaim: "not-a-number"));

        // Non-parseable claim should fall through; no header/session set → null
        Assert.Null(sut.TenantId);
    }

    // -----------------------------------------------------------------------
    // IsSuperAdmin tests
    // -----------------------------------------------------------------------

    [Fact]
    public void IsSuperAdmin_ReturnsFalse_WhenUserHasNoSuperAdminRole()
    {
        var sut = new TenantContext(BuildAccessor(isSuperAdmin: false));

        Assert.False(sut.IsSuperAdmin);
    }

    [Fact]
    public void IsSuperAdmin_ReturnsTrue_WhenUserIsInSuperAdminRole()
    {
        var sut = new TenantContext(BuildAccessor(isSuperAdmin: true));

        Assert.True(sut.IsSuperAdmin);
    }

    [Fact]
    public void IsSuperAdmin_ReturnsFalse_WhenHttpContextIsNull()
    {
        var accessor = new FakeHttpContextAccessor(null);
        var sut = new TenantContext(accessor);

        Assert.False(sut.IsSuperAdmin);
    }

    // -----------------------------------------------------------------------
    // SuperAdmin bypass semantics
    // -----------------------------------------------------------------------

    [Fact]
    public void SuperAdmin_WithNoTenantSource_HasNullTenantId_AllowingBypass()
    {
        // SuperAdmin with no tenant selected → null = "all tenants" bypass
        var sut = new TenantContext(BuildAccessor(isSuperAdmin: true));

        Assert.True(sut.IsSuperAdmin);
        Assert.Null(sut.TenantId);
    }

    [Fact]
    public void SuperAdmin_WithTenantSelected_ReturnsSelectedTenantId()
    {
        // SuperAdmin can still scope to a specific tenant when one is selected
        var sut = new TenantContext(BuildAccessor(isSuperAdmin: true, sessionTenant: 3));

        Assert.True(sut.IsSuperAdmin);
        Assert.Equal(3, sut.TenantId);
    }

    // -----------------------------------------------------------------------
    // Fake collaborators
    // -----------------------------------------------------------------------

    private sealed class FakeHttpContextAccessor(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }

    /// <summary>
    /// Minimal in-memory session that supports GetInt32/SetInt32.
    /// </summary>
    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "fake-session";
        public IEnumerable<string> Keys => _store.Keys;

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value)
            => _store.TryGetValue(key, out value!);

        public void Remove(string key) => _store.Remove(key);
        public void Clear() => _store.Clear();

        public Task LoadAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

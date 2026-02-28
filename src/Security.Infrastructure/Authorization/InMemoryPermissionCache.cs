using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Security.Application.Authorization;

namespace Security.Infrastructure.Authorization;

/// <summary>
/// In-memory permission cache keyed by (tenantId, userId).
/// Tenant-level invalidation is achieved via a <see cref="CancellationTokenSource"/>
/// per tenant: cancelling it causes IMemoryCache to evict all entries that were
/// registered with that token, without touching unrelated cache entries.
/// </summary>
public sealed class InMemoryPermissionCache(IMemoryCache cache) : IPermissionCache
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(15);

    // Tracks the CancellationTokenSource per tenant so InvalidateTenant/InvalidateAll
    // can cancel the right token(s) without enumerating IMemoryCache.
    private readonly Dictionary<string, CancellationTokenSource> _tenantTokens = [];
    private readonly object _tokenLock = new();

    private static string PermKey(int? tenantId, string userId)
        => $"perm:{tenantId}:{userId}";

    private static string TenantKey(int? tenantId)
        => tenantId?.ToString() ?? "null";

    private CancellationTokenSource GetOrCreateTenantCts(int? tenantId)
    {
        var key = TenantKey(tenantId);
        if (!_tenantTokens.TryGetValue(key, out var cts) || cts.IsCancellationRequested)
        {
            cts = new CancellationTokenSource();
            _tenantTokens[key] = cts;
        }

        return cts;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string>? Get(int? tenantId, string userId)
        => cache.TryGetValue(PermKey(tenantId, userId), out IReadOnlyList<string>? v) ? v : null;

    /// <inheritdoc/>
    public void Set(int? tenantId, string userId, IReadOnlyList<string> permissions)
    {
        CancellationTokenSource cts;
        lock (_tokenLock)
            cts = GetOrCreateTenantCts(tenantId);

        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DefaultExpiry)
            .AddExpirationToken(new CancellationChangeToken(cts.Token));

        cache.Set(PermKey(tenantId, userId), permissions, options);
    }

    /// <inheritdoc/>
    public void InvalidateUser(int? tenantId, string userId)
        => cache.Remove(PermKey(tenantId, userId));

    /// <inheritdoc/>
    public void InvalidateTenant(int? tenantId)
    {
        lock (_tokenLock)
        {
            var key = TenantKey(tenantId);
            if (_tenantTokens.TryGetValue(key, out var cts))
            {
                _tenantTokens.Remove(key);
                cts.Cancel();
                cts.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public void InvalidateAll()
    {
        lock (_tokenLock)
        {
            foreach (var cts in _tenantTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }

            _tenantTokens.Clear();
        }
    }
}

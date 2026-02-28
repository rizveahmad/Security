namespace Security.Application.Authorization;

/// <summary>
/// Abstraction for a tenant-aware, per-user permission set cache.
/// The cache key is composed of (tenantId, userId) so entries are fully
/// isolated across tenants.
/// </summary>
public interface IPermissionCache
{
    /// <summary>Returns the cached permission set for (tenantId, userId), or null when not cached.</summary>
    IReadOnlyList<string>? Get(int? tenantId, string userId);

    /// <summary>Stores the permission set for (tenantId, userId).</summary>
    void Set(int? tenantId, string userId, IReadOnlyList<string> permissions);

    /// <summary>Removes the cached entry for a specific user within a tenant.</summary>
    void InvalidateUser(int? tenantId, string userId);

    /// <summary>Removes all cached permission entries for a tenant.</summary>
    void InvalidateTenant(int? tenantId);

    /// <summary>Removes all cached permission entries across every tenant.</summary>
    void InvalidateAll();
}

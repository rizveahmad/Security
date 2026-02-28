namespace Security.Application.Interfaces;

/// <summary>
/// Provides the active tenant (company) for the current request.
/// Resolves from JWT claim 'tid', request header 'X-Tenant-Id', or session 'SelectedTenantId'.
/// A null TenantId means "all tenants" — only SuperAdmin is permitted to operate without a tenant.
/// </summary>
public interface ITenantContext
{
    /// <summary>Active tenant identifier, or null when SuperAdmin is viewing all tenants.</summary>
    int? TenantId { get; }

    /// <summary>True when the current user is a SuperAdmin (bypasses tenant isolation).</summary>
    bool IsSuperAdmin { get; }
}

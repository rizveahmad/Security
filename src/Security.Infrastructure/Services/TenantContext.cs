using Microsoft.AspNetCore.Http;
using Security.Application.Interfaces;
using System.Security.Claims;

namespace Security.Infrastructure.Services;

/// <summary>
/// Resolves the active tenant for the current HTTP request.
/// Resolution order:
///   1. JWT claim "tid" (for future API use)
///   2. Request header "X-Tenant-Id"
///   3. Session key "SelectedTenantId" (set via the admin tenant selector UI)
/// SuperAdmin users can operate without a selected tenant (TenantId == null = view all).
/// </summary>
public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public bool IsSuperAdmin =>
        httpContextAccessor.HttpContext?.User?.IsInRole("SuperAdmin") ?? false;

    public int? TenantId
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx == null) return null;

            // 1. Claim 'tid' (for API consumers)
            var tidClaim = ctx.User?.FindFirstValue("tid");
            if (int.TryParse(tidClaim, out var claimTenantId))
                return claimTenantId;

            // 2. Header 'X-Tenant-Id'
            if (ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue)
                && int.TryParse(headerValue.ToString(), out var headerTenantId))
                return headerTenantId;

            // 3. Session selection (from admin topbar dropdown)
            return ctx.Session.GetInt32("SelectedTenantId");
        }
    }
}

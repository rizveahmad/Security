using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Application.Authorization;

namespace Security.Web.Controllers.Api;

/// <summary>
/// Exposes the authenticated user's permission set (tenant-aware).
/// GET /api/permissions          – lists all permissions for the current user
/// GET /api/permissions/check    – checks a single permission code
/// Requires a valid Bearer JWT containing the <c>sub</c> claim (user id).
/// </summary>
[ApiController]
[Route("api/permissions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PermissionsApiController(IPermissionService permissionService) : ApiControllerBase
{
    /// <summary>
    /// Returns all permission codes held by the current user in the active tenant.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var userId = ResolveUserId();
        if (userId == null)
            return Unauthorized(new { error = "User identity not found in token." });

        var permissions = await permissionService.GetUserPermissionsAsync(userId, ct);
        return Ok(new { permissions });
    }

    /// <summary>
    /// Checks whether the current user holds a specific permission code.
    /// </summary>
    /// <param name="code">The permission code to test (e.g. "menu.view").</param>
    [HttpGet("check")]
    public async Task<IActionResult> CheckPermission([FromQuery] string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = "Query parameter 'code' is required." });

        var userId = ResolveUserId();
        if (userId == null)
            return Unauthorized(new { error = "User identity not found in token." });

        var hasPermission = await permissionService.HasPermissionAsync(userId, code, ct);
        return Ok(new { code, hasPermission });
    }
}

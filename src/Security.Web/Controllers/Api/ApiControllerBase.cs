using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Security.Web.Controllers.Api;

/// <summary>
/// Base class for API controllers that need to resolve the authenticated user's identity from a JWT.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Resolves the current user's ID from the JWT <c>sub</c> or <c>NameIdentifier</c> claim.
    /// Returns null when no valid user claim is present.
    /// </summary>
    protected string? ResolveUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}

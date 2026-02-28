using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Application.Features.Menus.Queries;

namespace Security.Web.Controllers.Api;

/// <summary>
/// Returns the menu tree for the authenticated user (tenant-aware).
/// GET /api/menu
/// Requires a valid Bearer JWT containing the <c>sub</c> claim (user id).
/// </summary>
[ApiController]
[Route("api/menu")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MenuApiController(IMediator mediator) : ApiControllerBase
{
    /// <summary>
    /// Returns the menu modules and their accessible menus for the current user.
    /// Only menus that the user has at least one matching permission for are included.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMenuTree(CancellationToken ct)
    {
        var userId = ResolveUserId();

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "User identity not found in token." });

        var tree = await mediator.Send(new GetUserMenuTreeQuery(userId), ct);
        return Ok(tree);
    }
}

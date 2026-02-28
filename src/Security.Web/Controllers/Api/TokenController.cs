using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Security.Application.Interfaces;
using Security.Infrastructure.Identity;

namespace Security.Web.Controllers.Api;

/// <summary>
/// Issues JWTs for external applications.
/// POST /api/auth/token
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class TokenController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    IConfiguration configuration) : ControllerBase
{
    public record TokenRequest(string Username, string Password, int? TenantId);
    public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

    /// <summary>
    /// Authenticates with username + password and returns a signed JWT.
    /// Optionally accepts a <c>tenantId</c> that is embedded in the <c>tid</c> claim.
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "username and password are required." });

        var user = await userManager.FindByNameAsync(request.Username)
                   ?? await userManager.FindByEmailAsync(request.Username);

        if (user == null || !user.IsActive)
            return Unauthorized(new { error = "Invalid credentials." });

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return StatusCode(StatusCodes.Status423Locked, new { error = "Account locked out." });

        if (!result.Succeeded)
            return Unauthorized(new { error = "Invalid credentials." });

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtTokenService.CreateToken(user.Id, user.Email, request.TenantId, roles);

        var expiresInSeconds = (int.TryParse(
            configuration["Jwt:ExpiresInMinutes"], out var mins) ? mins : 60) * 60;

        return Ok(new TokenResponse(token, JwtBearerDefaults.AuthenticationScheme, expiresInSeconds));
    }
}

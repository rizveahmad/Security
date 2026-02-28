namespace Security.Application.Interfaces;

/// <summary>
/// Issues a signed JWT for an authenticated user.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Creates a JWT for <paramref name="userId"/> scoped to <paramref name="tenantId"/>.
    /// The token carries: sub, tid, roles, and jti claims.
    /// </summary>
    string CreateToken(string userId, string? email, int? tenantId, IEnumerable<string> roles);
}

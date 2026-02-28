using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Security.Application.Interfaces;

namespace Security.Infrastructure.Identity;

/// <summary>
/// Creates signed JWTs for API authentication.
/// Configuration section "Jwt" must contain Key, Issuer, Audience, and ExpiresInMinutes.
/// </summary>
public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string CreateToken(string userId, string? email, int? tenantId, IEnumerable<string> roles)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured.");
        var issuer = jwtSection["Issuer"] ?? "SecurityApp";
        var audience = jwtSection["Audience"] ?? "SecurityApi";
        var expiresInMinutes = int.TryParse(jwtSection["ExpiresInMinutes"], out var mins) ? mins : 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));

        if (tenantId.HasValue)
            claims.Add(new Claim("tid", tenantId.Value.ToString()));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

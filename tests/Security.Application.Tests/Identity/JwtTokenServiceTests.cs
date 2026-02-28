using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Security.Infrastructure.Identity;

namespace Security.Application.Tests.Identity;

/// <summary>
/// Unit tests for JwtTokenService verifying claim contents and token validity.
/// </summary>
public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int expiresInMinutes = 60)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-that-is-long-enough-32chars",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpiresInMinutes"] = expiresInMinutes.ToString(),
            })
            .Build();

        return new JwtTokenService(config);
    }

    [Fact]
    public void CreateToken_ReturnsNonEmptyString()
    {
        var svc = CreateService();
        var token = svc.CreateToken("user1", "user@example.com", tenantId: 5, roles: ["Admin"]);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void CreateToken_SubClaimEqualsUserId()
    {
        var svc = CreateService();
        var token = svc.CreateToken("user123", null, null, []);
        var claims = ParseClaims(token);
        Assert.Equal("user123", claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
    }

    [Fact]
    public void CreateToken_TidClaimEqualsTenantId_WhenProvided()
    {
        var svc = CreateService();
        var token = svc.CreateToken("u1", null, tenantId: 42, roles: []);
        var claims = ParseClaims(token);
        Assert.Equal("42", claims.FindFirst("tid")?.Value);
    }

    [Fact]
    public void CreateToken_NoTidClaim_WhenTenantIdIsNull()
    {
        var svc = CreateService();
        var token = svc.CreateToken("u1", null, tenantId: null, roles: []);
        var claims = ParseClaims(token);
        Assert.Null(claims.FindFirst("tid"));
    }

    [Fact]
    public void CreateToken_RolesAreIncluded()
    {
        var svc = CreateService();
        var token = svc.CreateToken("u1", null, null, ["Admin", "User"]);
        var claims = ParseClaims(token);
        var roles = claims.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
    }

    [Fact]
    public void CreateToken_EmailClaimIncluded_WhenEmailProvided()
    {
        var svc = CreateService();
        var token = svc.CreateToken("u1", "test@example.com", null, []);
        var claims = ParseClaims(token);
        Assert.Equal("test@example.com", claims.FindFirst(JwtRegisteredClaimNames.Email)?.Value);
    }

    [Fact]
    public void CreateToken_ContainsJtiClaim()
    {
        var svc = CreateService();
        var token = svc.CreateToken("u1", null, null, []);
        var claims = ParseClaims(token);
        Assert.False(string.IsNullOrWhiteSpace(claims.FindFirst(JwtRegisteredClaimNames.Jti)?.Value));
    }

    private static ClaimsPrincipal ParseClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims));
    }
}

using Microsoft.AspNetCore.Identity;
using Security.Infrastructure.Identity;

namespace Security.Application.Tests.Identity;

/// <summary>
/// Unit tests for <see cref="CommonPasswordValidator"/>.
/// </summary>
public class CommonPasswordValidatorTests
{
    private readonly CommonPasswordValidator _validator = new();

    // CommonPasswordValidator does not use the UserManager or ApplicationUser arguments,
    // so null is safe to pass here.
    private static readonly UserManager<ApplicationUser> NullManager = null!;
    private static readonly ApplicationUser AnyUser = new();

    [Theory]
    [InlineData("password")]
    [InlineData("PASSWORD")]     // case-insensitive check
    [InlineData("Password123")]  // normalises to lowercase
    [InlineData("admin1234")]
    [InlineData("123456")]
    [InlineData("qwerty123")]
    public async Task Validate_RejectsCommonPasswords(string commonPassword)
    {
        var result = await _validator.ValidateAsync(NullManager, AnyUser, commonPassword);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "CommonPassword");
    }

    [Theory]
    [InlineData("MyStr0ng!Pass#2024")]
    [InlineData("Correct-Horse-Battery-Staple9!")]
    [InlineData("Xk7@mQ2#nPwZ")]
    public async Task Validate_AcceptsNonCommonPasswords(string uniquePassword)
    {
        var result = await _validator.ValidateAsync(NullManager, AnyUser, uniquePassword);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Validate_NullPassword_ReturnsSuccess()
    {
        // Null is handled by the built-in RequiredLength validator; CommonPasswordValidator should not throw.
        var result = await _validator.ValidateAsync(NullManager, AnyUser, null);

        Assert.True(result.Succeeded);
    }
}

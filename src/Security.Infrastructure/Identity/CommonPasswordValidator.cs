using Microsoft.AspNetCore.Identity;

namespace Security.Infrastructure.Identity;

/// <summary>
/// Rejects passwords that appear in a list of commonly breached or trivially guessable passwords.
/// This validator is registered alongside the built-in Identity password validators and runs
/// after the length / complexity checks.
/// </summary>
public sealed class CommonPasswordValidator : IPasswordValidator<ApplicationUser>
{
    // A curated subset of the most commonly breached passwords.
    // Extend this list or replace it with a file-based check as needed.
    private static readonly HashSet<string> CommonPasswords =
    [
        "password", "password1", "password12", "password123",
        "p@ssword", "p@ssword1", "p@ssw0rd", "p@$$w0rd",
        "qwerty", "qwerty123", "qwerty@123",
        "letmein", "welcome1", "welcome@1",
        "admin", "admin1234", "admin@1234",
        "changeme", "changeme1",
        "iloveyou", "sunshine", "princess",
        "trustno1", "dragon", "master",
        "123456", "1234567", "12345678", "123456789", "1234567890",
        "11111111", "000000000",
        "abc123", "abc@1234",
        "monkey", "shadow", "superman",
        "passw0rd", "pass@word1", "pass@word",
        "security", "security1", "security@1",
    ];

    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        if (password is not null &&
            CommonPasswords.Contains(password.ToLowerInvariant()))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "CommonPassword",
                Description = "The chosen password is too common. Please choose a more unique password.",
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}

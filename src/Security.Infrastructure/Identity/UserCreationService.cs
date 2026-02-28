using Microsoft.AspNetCore.Identity;
using Security.Application.Common.Interfaces;

namespace Security.Infrastructure.Identity;

public class UserCreationService(UserManager<ApplicationUser> userManager) : IUserCreationService
{
    public async Task<string> CreateUserAsync(string email, string firstName, string lastName, string password, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedBy = "system",
            CreatedDate = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }
}

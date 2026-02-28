using Microsoft.AspNetCore.Identity;

namespace Security.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}

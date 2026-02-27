using Microsoft.AspNetCore.Identity;
using Security.Domain.Common;

namespace Security.Infrastructure.Identity;

/// <summary>
/// Application user that extends ASP.NET Core Identity with audit fields.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public bool IsActive { get; set; } = true;
}

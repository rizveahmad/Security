using Microsoft.AspNetCore.Identity;

namespace Security.Domain.Entities;

public class Role : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

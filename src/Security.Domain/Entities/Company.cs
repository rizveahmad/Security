using Security.Domain.Common;

namespace Security.Domain.Entities;
public class Company : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<AppModule> Modules { get; set; } = new List<AppModule>();
}

using Security.Domain.Common;

namespace Security.Domain.Entities;
public class Workstation : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? IPAddress { get; set; }
    public string? MACAddress { get; set; }
    public bool IsActive { get; set; } = true;
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
}

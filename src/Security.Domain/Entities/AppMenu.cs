using Security.Domain.Common;

namespace Security.Domain.Entities;
public class AppMenu : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int ModuleId { get; set; }
    public AppModule? Module { get; set; }
    public ICollection<PermissionType> PermissionTypes { get; set; } = new List<PermissionType>();
}

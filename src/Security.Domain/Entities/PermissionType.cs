using Security.Domain.Common;

namespace Security.Domain.Entities;
public class PermissionType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int MenuId { get; set; }
    public AppMenu? Menu { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

using Security.Domain.Common;

namespace Security.Domain.Entities;
public class AppRole : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    public ICollection<RoleGroupRole> RoleGroupRoles { get; set; } = new List<RoleGroupRole>();
}

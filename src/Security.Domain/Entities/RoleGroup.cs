using Security.Domain.Common;

namespace Security.Domain.Entities;
public class RoleGroup : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public ICollection<RoleGroupRole> Roles { get; set; } = new List<RoleGroupRole>();
    public ICollection<UserRoleGroup> UserAssignments { get; set; } = new List<UserRoleGroup>();
}

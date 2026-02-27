using Security.Domain.Common;

namespace Security.Domain.Entities;
public class RoleGroupRole : AuditableEntity
{
    public int RoleGroupId { get; set; }
    public RoleGroup? RoleGroup { get; set; }
    public int RoleId { get; set; }
    public AppRole? Role { get; set; }
}

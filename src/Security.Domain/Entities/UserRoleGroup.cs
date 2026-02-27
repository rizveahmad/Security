using Security.Domain.Common;

namespace Security.Domain.Entities;
public class UserRoleGroup : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public int RoleGroupId { get; set; }
    public RoleGroup? RoleGroup { get; set; }
}

using Security.Domain.Common;

namespace Security.Domain.Entities;
public class RolePermission : AuditableEntity
{
    public int RoleId { get; set; }
    public AppRole? Role { get; set; }
    public int PermissionTypeId { get; set; }
    public PermissionType? PermissionType { get; set; }
}

using Microsoft.AspNetCore.Authorization;

namespace Security.Application.Authorization;

/// <summary>
/// Authorization requirement representing a specific permission (PermissionType code).
/// </summary>
public class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}

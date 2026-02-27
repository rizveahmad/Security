namespace Security.Application.Authorization;

/// <summary>
/// Evaluates whether a given user has a specific permission based on DB-stored role/role-group assignments.
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string userId, string permissionCode, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(string userId, CancellationToken ct = default);
}

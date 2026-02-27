using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Roles.Queries;

public record GetRoleByIdQuery(int Id) : IRequest<RoleDetailDto?>;

public record RoleDetailDto(int Id, string Name, string Code, string? Description, bool IsActive, int CompanyId, string? CompanyName, List<int> PermissionTypeIds);

public class GetRoleByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRoleByIdQuery, RoleDetailDto?>
{
    public async Task<RoleDetailDto?> Handle(GetRoleByIdQuery request, CancellationToken ct)
    {
        var role = await context.AppRoles.AsNoTracking()
            .Include(r => r.Company)
            .Include(r => r.Permissions)
            .Where(r => r.Id == request.Id)
            .FirstOrDefaultAsync(ct);
        if (role is null) return null;
        var permIds = role.Permissions.Select(p => p.PermissionTypeId).ToList();
        return new RoleDetailDto(role.Id, role.Name, role.Code, role.Description, role.IsActive, role.CompanyId, role.Company?.Name, permIds);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.RoleGroups.Queries;

public record GetRoleGroupByIdQuery(int Id) : IRequest<RoleGroupDetailDto?>;

public record RoleGroupDetailDto(int Id, string Name, string? Code, string? Description, bool IsActive, int CompanyId, string? CompanyName, List<int> RoleIds);

public class GetRoleGroupByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRoleGroupByIdQuery, RoleGroupDetailDto?>
{
    public async Task<RoleGroupDetailDto?> Handle(GetRoleGroupByIdQuery request, CancellationToken ct)
    {
        var rg = await context.RoleGroups.AsNoTracking()
            .Include(r => r.Company).Include(r => r.Roles)
            .Where(r => r.Id == request.Id).FirstOrDefaultAsync(ct);
        if (rg is null) return null;
        var roleIds = rg.Roles.Select(r => r.RoleId).ToList();
        return new RoleGroupDetailDto(rg.Id, rg.Name, rg.Code, rg.Description, rg.IsActive, rg.CompanyId, rg.Company?.Name, roleIds);
    }
}

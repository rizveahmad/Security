using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;

namespace Security.Application.Features.RoleGroups.Queries;

public record GetRoleGroupsQuery(int PageNumber = 1, int PageSize = 10, string? Search = null, int? CompanyId = null)
    : IRequest<PaginatedList<RoleGroupDto>>;

public record RoleGroupDto(int Id, string Name, string? Code, string? Description, bool IsActive, int CompanyId, string? CompanyName, int RoleCount);

public class GetRoleGroupsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRoleGroupsQuery, PaginatedList<RoleGroupDto>>
{
    public async Task<PaginatedList<RoleGroupDto>> Handle(GetRoleGroupsQuery request, CancellationToken ct)
    {
        IQueryable<Security.Domain.Entities.RoleGroup> query = context.RoleGroups.AsNoTracking().Include(rg => rg.Company).Include(rg => rg.Roles);
        if (request.CompanyId.HasValue) query = query.Where(rg => rg.CompanyId == request.CompanyId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(rg => rg.Name.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(rg => rg.Name)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(rg => new RoleGroupDto(rg.Id, rg.Name, rg.Code, rg.Description, rg.IsActive, rg.CompanyId, rg.Company != null ? rg.Company.Name : null, rg.Roles.Count()))
            .ToListAsync(ct);
        return new PaginatedList<RoleGroupDto>(items, total, request.PageNumber, request.PageSize);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;

namespace Security.Application.Features.Roles.Queries;

public record GetRolesQuery(int PageNumber = 1, int PageSize = 10, string? Search = null, int? CompanyId = null)
    : IRequest<PaginatedList<RoleDto>>;

public record RoleDto(int Id, string Name, string Code, string? Description, bool IsActive, int CompanyId, string? CompanyName);

public class GetRolesQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRolesQuery, PaginatedList<RoleDto>>
{
    public async Task<PaginatedList<RoleDto>> Handle(GetRolesQuery request, CancellationToken ct)
    {
        IQueryable<Security.Domain.Entities.AppRole> query = context.AppRoles.AsNoTracking().Include(r => r.Company);
        if (request.CompanyId.HasValue) query = query.Where(r => r.CompanyId == request.CompanyId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(r => r.Name.Contains(request.Search) || r.Code.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(r => r.Name)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(r => new RoleDto(r.Id, r.Name, r.Code, r.Description, r.IsActive, r.CompanyId, r.Company != null ? r.Company.Name : null))
            .ToListAsync(ct);
        return new PaginatedList<RoleDto>(items, total, request.PageNumber, request.PageSize);
    }
}

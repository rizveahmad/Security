using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;

namespace Security.Application.Features.PermissionTypes.Queries;

public record GetPermissionTypesQuery(int PageNumber = 1, int PageSize = 10, string? Search = null, int? MenuId = null)
    : IRequest<PaginatedList<PermissionTypeDto>>;

public record PermissionTypeDto(int Id, string Name, string? Code, string? Description, bool IsActive, int MenuId, string? MenuName);

public class GetPermissionTypesQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPermissionTypesQuery, PaginatedList<PermissionTypeDto>>
{
    public async Task<PaginatedList<PermissionTypeDto>> Handle(GetPermissionTypesQuery request, CancellationToken ct)
    {
        IQueryable<Security.Domain.Entities.PermissionType> query = context.PermissionTypes.AsNoTracking().Include(p => p.Menu);
        if (request.MenuId.HasValue) query = query.Where(p => p.MenuId == request.MenuId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(p => p.Name.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => new PermissionTypeDto(p.Id, p.Name, p.Code, p.Description, p.IsActive, p.MenuId, p.Menu != null ? p.Menu.Name : null))
            .ToListAsync(ct);
        return new PaginatedList<PermissionTypeDto>(items, total, request.PageNumber, request.PageSize);
    }
}

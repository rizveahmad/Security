using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;

namespace Security.Application.Features.Menus.Queries;

public record GetMenusQuery(int PageNumber = 1, int PageSize = 10, string? Search = null, int? ModuleId = null)
    : IRequest<PaginatedList<MenuDto>>;

public record MenuDto(int Id, string Name, string? Code, string? Url, string? Icon, int DisplayOrder, bool IsActive, int ModuleId, string? ModuleName);

public class GetMenusQueryHandler(IApplicationDbContext context) : IRequestHandler<GetMenusQuery, PaginatedList<MenuDto>>
{
    public async Task<PaginatedList<MenuDto>> Handle(GetMenusQuery request, CancellationToken ct)
    {
        IQueryable<Security.Domain.Entities.AppMenu> query = context.AppMenus.AsNoTracking().Include(m => m.Module);
        if (request.ModuleId.HasValue) query = query.Where(m => m.ModuleId == request.ModuleId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(m => m.Name.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(m => new MenuDto(m.Id, m.Name, m.Code, m.Url, m.Icon, m.DisplayOrder, m.IsActive, m.ModuleId, m.Module != null ? m.Module.Name : null))
            .ToListAsync(ct);
        return new PaginatedList<MenuDto>(items, total, request.PageNumber, request.PageSize);
    }
}

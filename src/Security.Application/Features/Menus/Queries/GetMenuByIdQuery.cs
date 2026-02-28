using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Menus.Queries;

public record GetMenuByIdQuery(int Id) : IRequest<MenuDto?>;

public class GetMenuByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetMenuByIdQuery, MenuDto?>
{
    public async Task<MenuDto?> Handle(GetMenuByIdQuery request, CancellationToken ct)
        => await context.AppMenus.AsNoTracking().Include(m => m.Module)
            .Where(m => m.Id == request.Id)
            .Select(m => new MenuDto(m.Id, m.Name, m.Code, m.Url, m.Icon, m.DisplayOrder, m.IsActive, m.ModuleId, m.Module != null ? m.Module.Name : null))
            .FirstOrDefaultAsync(ct);
}

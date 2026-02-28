using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Roles.Queries;

public record GetPermissionTreeQuery(int CompanyId) : IRequest<List<PermissionTreeModuleDto>>;

public record PermissionTreePermissionDto(int Id, string Name, string? Code);
public record PermissionTreeMenuDto(int Id, string Name, string? Code, List<PermissionTreePermissionDto> Permissions);
public record PermissionTreeModuleDto(int Id, string Name, string? Code, List<PermissionTreeMenuDto> Menus);

public class GetPermissionTreeQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPermissionTreeQuery, List<PermissionTreeModuleDto>>
{
    public async Task<List<PermissionTreeModuleDto>> Handle(GetPermissionTreeQuery request, CancellationToken ct)
    {
        var modules = await context.AppModules.AsNoTracking()
            .Where(m => m.CompanyId == request.CompanyId)
            .Include(m => m.Menus)
                .ThenInclude(menu => menu.PermissionTypes)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);

        return modules.Select(m => new PermissionTreeModuleDto(m.Id, m.Name, m.Code,
            m.Menus.OrderBy(mn => mn.DisplayOrder).Select(mn => new PermissionTreeMenuDto(mn.Id, mn.Name, mn.Code,
                mn.PermissionTypes.OrderBy(pt => pt.Name).Select(pt => new PermissionTreePermissionDto(pt.Id, pt.Name, pt.Code)).ToList()
            )).ToList()
        )).ToList();
    }
}

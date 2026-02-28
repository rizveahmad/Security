using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;

namespace Security.Application.Features.Modules.Queries;

public record GetModulesQuery(int PageNumber = 1, int PageSize = 10, string? Search = null, int? CompanyId = null)
    : IRequest<PaginatedList<ModuleDto>>;

public record ModuleDto(int Id, string Name, string? Code, string? Description, bool IsActive, int CompanyId, string? CompanyName);

public class GetModulesQueryHandler(IApplicationDbContext context) : IRequestHandler<GetModulesQuery, PaginatedList<ModuleDto>>
{
    public async Task<PaginatedList<ModuleDto>> Handle(GetModulesQuery request, CancellationToken ct)
    {
        IQueryable<Security.Domain.Entities.AppModule> query = context.AppModules.AsNoTracking().Include(m => m.Company);
        if (request.CompanyId.HasValue)
            query = query.Where(m => m.CompanyId == request.CompanyId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(m => m.Name.Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new ModuleDto(m.Id, m.Name, m.Code, m.Description, m.IsActive, m.CompanyId, m.Company != null ? m.Company.Name : null))
            .ToListAsync(ct);

        return new PaginatedList<ModuleDto>(items, total, request.PageNumber, request.PageSize);
    }
}

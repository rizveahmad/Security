using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;

namespace Security.Application.Features.Companies.Queries;

public record GetCompaniesQuery(int PageNumber = 1, int PageSize = 10, string? Search = null)
    : IRequest<PaginatedList<CompanyDto>>;

public record CompanyDto(int Id, string Name, string? Code, string? Address, bool IsActive);

public class GetCompaniesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetCompaniesQuery, PaginatedList<CompanyDto>>
{
    public async Task<PaginatedList<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken ct)
    {
        var query = context.Companies.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(c => c.Name.Contains(request.Search) || (c.Code != null && c.Code.Contains(request.Search)));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CompanyDto(c.Id, c.Name, c.Code, c.Address, c.IsActive))
            .ToListAsync(ct);

        return new PaginatedList<CompanyDto>(items, total, request.PageNumber, request.PageSize);
    }
}

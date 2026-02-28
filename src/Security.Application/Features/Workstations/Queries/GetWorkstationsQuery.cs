using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;

namespace Security.Application.Features.Workstations.Queries;

public record GetWorkstationsQuery(int PageNumber = 1, int PageSize = 10, string? Search = null, int? CompanyId = null)
    : IRequest<PaginatedList<WorkstationDto>>;

public record WorkstationDto(int Id, string Name, string? Code, string? IPAddress, string? MACAddress, bool IsActive, int CompanyId, string? CompanyName);

public class GetWorkstationsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetWorkstationsQuery, PaginatedList<WorkstationDto>>
{
    public async Task<PaginatedList<WorkstationDto>> Handle(GetWorkstationsQuery request, CancellationToken ct)
    {
        IQueryable<Security.Domain.Entities.Workstation> query = context.Workstations.AsNoTracking().Include(w => w.Company);
        if (request.CompanyId.HasValue) query = query.Where(w => w.CompanyId == request.CompanyId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(w => w.Name.Contains(request.Search) || (w.IPAddress != null && w.IPAddress.Contains(request.Search)));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(w => w.Name)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(w => new WorkstationDto(w.Id, w.Name, w.Code, w.IPAddress, w.MACAddress, w.IsActive, w.CompanyId, w.Company != null ? w.Company.Name : null))
            .ToListAsync(ct);
        return new PaginatedList<WorkstationDto>(items, total, request.PageNumber, request.PageSize);
    }
}

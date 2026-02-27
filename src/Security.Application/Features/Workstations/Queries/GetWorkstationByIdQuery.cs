using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Workstations.Queries;

public record GetWorkstationByIdQuery(int Id) : IRequest<WorkstationDto?>;

public class GetWorkstationByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetWorkstationByIdQuery, WorkstationDto?>
{
    public async Task<WorkstationDto?> Handle(GetWorkstationByIdQuery request, CancellationToken ct)
        => await context.Workstations.AsNoTracking().Include(w => w.Company)
            .Where(w => w.Id == request.Id)
            .Select(w => new WorkstationDto(w.Id, w.Name, w.Code, w.IPAddress, w.MACAddress, w.IsActive, w.CompanyId, w.Company != null ? w.Company.Name : null))
            .FirstOrDefaultAsync(ct);
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Modules.Queries;

public record GetModuleByIdQuery(int Id) : IRequest<ModuleDto?>;

public class GetModuleByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetModuleByIdQuery, ModuleDto?>
{
    public async Task<ModuleDto?> Handle(GetModuleByIdQuery request, CancellationToken ct)
        => await context.AppModules.AsNoTracking()
            .Include(m => m.Company)
            .Where(m => m.Id == request.Id)
            .Select(m => new ModuleDto(m.Id, m.Name, m.Code, m.Description, m.IsActive, m.CompanyId, m.Company != null ? m.Company.Name : null))
            .FirstOrDefaultAsync(ct);
}

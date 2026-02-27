using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.PermissionTypes.Queries;

public record GetPermissionTypeByIdQuery(int Id) : IRequest<PermissionTypeDto?>;

public class GetPermissionTypeByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPermissionTypeByIdQuery, PermissionTypeDto?>
{
    public async Task<PermissionTypeDto?> Handle(GetPermissionTypeByIdQuery request, CancellationToken ct)
        => await context.PermissionTypes.AsNoTracking().Include(p => p.Menu)
            .Where(p => p.Id == request.Id)
            .Select(p => new PermissionTypeDto(p.Id, p.Name, p.Code, p.Description, p.IsActive, p.MenuId, p.Menu != null ? p.Menu.Name : null))
            .FirstOrDefaultAsync(ct);
}

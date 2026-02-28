using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Authorization;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.PermissionTypes.Commands;

public record DeletePermissionTypeCommand(int Id) : IRequest<bool>;

public class DeletePermissionTypeCommandHandler(
    IApplicationDbContext context,
    IPermissionCache permissionCache) : IRequestHandler<DeletePermissionTypeCommand, bool>
{
    public async Task<bool> Handle(DeletePermissionTypeCommand request, CancellationToken ct)
    {
        var entity = await context.PermissionTypes.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete("system");
        await context.SaveChangesAsync(ct);

        // PermissionTypes are not directly scoped to a single tenant, so invalidate all.
        permissionCache.InvalidateAll();
        return true;
    }
}

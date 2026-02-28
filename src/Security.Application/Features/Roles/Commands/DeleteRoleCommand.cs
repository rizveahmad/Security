using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Authorization;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Roles.Commands;

public record DeleteRoleCommand(int Id) : IRequest<bool>;

public class DeleteRoleCommandHandler(
    IApplicationDbContext context,
    IPermissionCache permissionCache) : IRequestHandler<DeleteRoleCommand, bool>
{
    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken ct)
    {
        var entity = await context.AppRoles.FirstOrDefaultAsync(r => r.Id == request.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete("system");
        await context.SaveChangesAsync(ct);

        permissionCache.InvalidateTenant(entity.CompanyId);
        return true;
    }
}

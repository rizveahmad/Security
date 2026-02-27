using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.RoleGroups.Commands;

public record DeleteRoleGroupCommand(int Id) : IRequest<bool>;

public class DeleteRoleGroupCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteRoleGroupCommand, bool>
{
    public async Task<bool> Handle(DeleteRoleGroupCommand request, CancellationToken ct)
    {
        var entity = await context.RoleGroups.FirstOrDefaultAsync(rg => rg.Id == request.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete("system");
        await context.SaveChangesAsync(ct);
        return true;
    }
}

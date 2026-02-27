using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.PermissionTypes.Commands;

public record DeletePermissionTypeCommand(int Id) : IRequest<bool>;

public class DeletePermissionTypeCommandHandler(IApplicationDbContext context) : IRequestHandler<DeletePermissionTypeCommand, bool>
{
    public async Task<bool> Handle(DeletePermissionTypeCommand request, CancellationToken ct)
    {
        var entity = await context.PermissionTypes.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete("system");
        await context.SaveChangesAsync(ct);
        return true;
    }
}

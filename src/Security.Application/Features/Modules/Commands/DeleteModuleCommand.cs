using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Modules.Commands;

public record DeleteModuleCommand(int Id) : IRequest<bool>;

public class DeleteModuleCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteModuleCommand, bool>
{
    public async Task<bool> Handle(DeleteModuleCommand request, CancellationToken ct)
    {
        var entity = await context.AppModules.FirstOrDefaultAsync(m => m.Id == request.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete("system");
        await context.SaveChangesAsync(ct);
        return true;
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Workstations.Commands;

public record DeleteWorkstationCommand(int Id) : IRequest<bool>;

public class DeleteWorkstationCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteWorkstationCommand, bool>
{
    public async Task<bool> Handle(DeleteWorkstationCommand request, CancellationToken ct)
    {
        var entity = await context.Workstations.FirstOrDefaultAsync(w => w.Id == request.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete("system");
        await context.SaveChangesAsync(ct);
        return true;
    }
}

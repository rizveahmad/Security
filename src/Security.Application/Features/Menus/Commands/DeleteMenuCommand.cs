using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Menus.Commands;

public record DeleteMenuCommand(int Id) : IRequest<bool>;

public class DeleteMenuCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteMenuCommand, bool>
{
    public async Task<bool> Handle(DeleteMenuCommand request, CancellationToken ct)
    {
        var entity = await context.AppMenus.FirstOrDefaultAsync(m => m.Id == request.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete("system");
        await context.SaveChangesAsync(ct);
        return true;
    }
}

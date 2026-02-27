using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Companies.Commands;

public record DeleteCompanyCommand(int Id) : IRequest<bool>;

public class DeleteCompanyCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteCompanyCommand, bool>
{
    public async Task<bool> Handle(DeleteCompanyCommand request, CancellationToken ct)
    {
        var entity = await context.Companies.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (entity is null) return false;
        entity.SoftDelete("system");
        await context.SaveChangesAsync(ct);
        return true;
    }
}

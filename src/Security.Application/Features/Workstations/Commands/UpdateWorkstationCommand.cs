using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Workstations.Commands;

public record UpdateWorkstationCommand(int Id, string Name, string? Code, string? IPAddress, string? MACAddress, int CompanyId, bool IsActive) : IRequest<bool>;

public class UpdateWorkstationCommandValidator : AbstractValidator<UpdateWorkstationCommand>
{
    public UpdateWorkstationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public class UpdateWorkstationCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateWorkstationCommand, bool>
{
    public async Task<bool> Handle(UpdateWorkstationCommand request, CancellationToken ct)
    {
        var entity = await context.Workstations.FirstOrDefaultAsync(w => w.Id == request.Id, ct);
        if (entity is null) return false;
        entity.Name = request.Name; entity.Code = request.Code; entity.IPAddress = request.IPAddress; entity.MACAddress = request.MACAddress;
        entity.CompanyId = request.CompanyId; entity.IsActive = request.IsActive;
        entity.UpdatedDate = DateTime.UtcNow; entity.UpdatedBy = "system";
        await context.SaveChangesAsync(ct);
        return true;
    }
}

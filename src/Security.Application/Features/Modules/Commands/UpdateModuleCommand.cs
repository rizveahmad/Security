using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Modules.Commands;

public record UpdateModuleCommand(int Id, string Name, string? Code, string? Description, int CompanyId, bool IsActive) : IRequest<bool>;

public class UpdateModuleCommandValidator : AbstractValidator<UpdateModuleCommand>
{
    public UpdateModuleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public class UpdateModuleCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateModuleCommand, bool>
{
    public async Task<bool> Handle(UpdateModuleCommand request, CancellationToken ct)
    {
        var entity = await context.AppModules.FirstOrDefaultAsync(m => m.Id == request.Id, ct);
        if (entity is null) return false;
        entity.Name = request.Name; entity.Code = request.Code; entity.Description = request.Description;
        entity.CompanyId = request.CompanyId; entity.IsActive = request.IsActive;
        entity.UpdatedDate = DateTime.UtcNow; entity.UpdatedBy = "system";
        await context.SaveChangesAsync(ct);
        return true;
    }
}

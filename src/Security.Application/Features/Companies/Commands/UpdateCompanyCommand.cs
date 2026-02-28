using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Companies.Commands;

public record UpdateCompanyCommand(int Id, string Name, string? Code, string? Address, bool IsActive) : IRequest<bool>;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
    }
}

public class UpdateCompanyCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateCompanyCommand, bool>
{
    public async Task<bool> Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        var entity = await context.Companies.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (entity is null) return false;
        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.Address = request.Address;
        entity.IsActive = request.IsActive;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.UpdatedBy = "system";
        await context.SaveChangesAsync(ct);
        return true;
    }
}

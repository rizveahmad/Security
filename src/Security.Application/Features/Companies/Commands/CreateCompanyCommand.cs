using FluentValidation;
using MediatR;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.Companies.Commands;

public record CreateCompanyCommand(string Name, string? Code, string? Address, bool IsActive = true) : IRequest<int>;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
    }
}

public class CreateCompanyCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateCompanyCommand, int>
{
    public async Task<int> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        var entity = new Company
        {
            Name = request.Name,
            Code = request.Code,
            Address = request.Address,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system"
        };
        context.Companies.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity.Id;
    }
}

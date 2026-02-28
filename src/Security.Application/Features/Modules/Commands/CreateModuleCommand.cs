using FluentValidation;
using MediatR;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.Modules.Commands;

public record CreateModuleCommand(string Name, string? Code, string? Description, int CompanyId, bool IsActive = true) : IRequest<int>;

public class CreateModuleCommandValidator : AbstractValidator<CreateModuleCommand>
{
    public CreateModuleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public class CreateModuleCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateModuleCommand, int>
{
    public async Task<int> Handle(CreateModuleCommand request, CancellationToken ct)
    {
        var entity = new AppModule { Name = request.Name, Code = request.Code, Description = request.Description, CompanyId = request.CompanyId, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "system" };
        context.AppModules.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity.Id;
    }
}

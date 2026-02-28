using FluentValidation;
using MediatR;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.PermissionTypes.Commands;

public record CreatePermissionTypeCommand(string Name, string? Code, string? Description, int MenuId, bool IsActive = true) : IRequest<int>;

public class CreatePermissionTypeCommandValidator : AbstractValidator<CreatePermissionTypeCommand>
{
    public CreatePermissionTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MenuId).GreaterThan(0);
    }
}

public class CreatePermissionTypeCommandHandler(IApplicationDbContext context) : IRequestHandler<CreatePermissionTypeCommand, int>
{
    public async Task<int> Handle(CreatePermissionTypeCommand request, CancellationToken ct)
    {
        var entity = new PermissionType { Name = request.Name, Code = request.Code, Description = request.Description, MenuId = request.MenuId, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "system" };
        context.PermissionTypes.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity.Id;
    }
}

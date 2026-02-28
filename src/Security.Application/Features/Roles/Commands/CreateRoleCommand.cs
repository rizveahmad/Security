using FluentValidation;
using MediatR;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.Roles.Commands;

public record CreateRoleCommand(string Name, string Code, string? Description, int CompanyId, bool IsActive, List<int> PermissionTypeIds) : IRequest<int>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public class CreateRoleCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateRoleCommand, int>
{
    public async Task<int> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        var entity = new AppRole { Name = request.Name, Code = request.Code, Description = request.Description, CompanyId = request.CompanyId, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "system" };
        foreach (var ptId in request.PermissionTypeIds)
            entity.Permissions.Add(new RolePermission { PermissionTypeId = ptId, CreatedDate = DateTime.UtcNow, CreatedBy = "system" });
        context.AppRoles.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity.Id;
    }
}

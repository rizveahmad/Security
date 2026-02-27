using FluentValidation;
using MediatR;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.RoleGroups.Commands;

public record CreateRoleGroupCommand(string Name, string? Code, string? Description, int CompanyId, bool IsActive, List<int> RoleIds) : IRequest<int>;

public class CreateRoleGroupCommandValidator : AbstractValidator<CreateRoleGroupCommand>
{
    public CreateRoleGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public class CreateRoleGroupCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateRoleGroupCommand, int>
{
    public async Task<int> Handle(CreateRoleGroupCommand request, CancellationToken ct)
    {
        var entity = new RoleGroup { Name = request.Name, Code = request.Code, Description = request.Description, CompanyId = request.CompanyId, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "system" };
        foreach (var roleId in request.RoleIds)
            entity.Roles.Add(new RoleGroupRole { RoleId = roleId, CreatedDate = DateTime.UtcNow, CreatedBy = "system" });
        context.RoleGroups.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity.Id;
    }
}

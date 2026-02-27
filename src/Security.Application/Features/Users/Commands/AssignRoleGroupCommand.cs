using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.Users.Commands;

public record AssignRoleGroupCommand(string UserId, int RoleGroupId) : IRequest<bool>;

public class AssignRoleGroupCommandValidator : AbstractValidator<AssignRoleGroupCommand>
{
    public AssignRoleGroupCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleGroupId).GreaterThan(0);
    }
}

public class AssignRoleGroupCommandHandler(IApplicationDbContext context) : IRequestHandler<AssignRoleGroupCommand, bool>
{
    public async Task<bool> Handle(AssignRoleGroupCommand request, CancellationToken ct)
    {
        var existing = await context.UserRoleGroups.Where(u => u.UserId == request.UserId).ToListAsync(ct);
        foreach (var e in existing) e.SoftDelete("system");

        context.UserRoleGroups.Add(new UserRoleGroup { UserId = request.UserId, RoleGroupId = request.RoleGroupId, CreatedDate = DateTime.UtcNow, CreatedBy = "system" });
        await context.SaveChangesAsync(ct);
        return true;
    }
}

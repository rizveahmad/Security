using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Authorization;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.RoleGroups.Commands;

public record UpdateRoleGroupCommand(int Id, string Name, string? Code, string? Description, int CompanyId, bool IsActive, List<int> RoleIds) : IRequest<bool>;

public class UpdateRoleGroupCommandValidator : AbstractValidator<UpdateRoleGroupCommand>
{
    public UpdateRoleGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public class UpdateRoleGroupCommandHandler(
    IApplicationDbContext context,
    IPermissionCache permissionCache) : IRequestHandler<UpdateRoleGroupCommand, bool>
{
    public async Task<bool> Handle(UpdateRoleGroupCommand request, CancellationToken ct)
    {
        var entity = await context.RoleGroups.Include(rg => rg.Roles).FirstOrDefaultAsync(rg => rg.Id == request.Id, ct);
        if (entity is null) return false;
        entity.Name = request.Name; entity.Code = request.Code; entity.Description = request.Description;
        entity.CompanyId = request.CompanyId; entity.IsActive = request.IsActive;
        entity.UpdatedDate = DateTime.UtcNow; entity.UpdatedBy = "system";

        var existing = entity.Roles.Select(r => r.RoleId).ToHashSet();
        var requested = request.RoleIds.ToHashSet();
        foreach (var r in entity.Roles.Where(r => !requested.Contains(r.RoleId)))
            r.SoftDelete("system");
        foreach (var newId in requested.Where(id => !existing.Contains(id)))
            entity.Roles.Add(new RoleGroupRole { RoleId = newId, CreatedDate = DateTime.UtcNow, CreatedBy = "system" });

        await context.SaveChangesAsync(ct);

        permissionCache.InvalidateTenant(request.CompanyId);
        return true;
    }
}

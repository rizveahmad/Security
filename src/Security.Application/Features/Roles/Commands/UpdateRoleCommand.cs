using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Authorization;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.Roles.Commands;

public record UpdateRoleCommand(int Id, string Name, string Code, string? Description, int CompanyId, bool IsActive, List<int> PermissionTypeIds) : IRequest<bool>;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public class UpdateRoleCommandHandler(
    IApplicationDbContext context,
    IPermissionCache permissionCache) : IRequestHandler<UpdateRoleCommand, bool>
{
    public async Task<bool> Handle(UpdateRoleCommand request, CancellationToken ct)
    {
        var entity = await context.AppRoles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == request.Id, ct);
        if (entity is null) return false;
        entity.Name = request.Name; entity.Code = request.Code; entity.Description = request.Description;
        entity.CompanyId = request.CompanyId; entity.IsActive = request.IsActive;
        entity.UpdatedDate = DateTime.UtcNow; entity.UpdatedBy = "system";

        var existingPermIds = entity.Permissions.Select(p => p.PermissionTypeId).ToHashSet();
        var requestPermIds = request.PermissionTypeIds.ToHashSet();

        foreach (var perm in entity.Permissions.Where(p => !requestPermIds.Contains(p.PermissionTypeId)))
            perm.SoftDelete("system");

        foreach (var newId in requestPermIds.Where(id => !existingPermIds.Contains(id)))
            entity.Permissions.Add(new RolePermission { PermissionTypeId = newId, CreatedDate = DateTime.UtcNow, CreatedBy = "system" });

        await context.SaveChangesAsync(ct);

        permissionCache.InvalidateTenant(request.CompanyId);
        return true;
    }
}

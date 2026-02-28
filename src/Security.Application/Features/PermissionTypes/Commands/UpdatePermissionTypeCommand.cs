using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Authorization;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.PermissionTypes.Commands;

public record UpdatePermissionTypeCommand(int Id, string Name, string? Code, string? Description, int MenuId, bool IsActive) : IRequest<bool>;

public class UpdatePermissionTypeCommandValidator : AbstractValidator<UpdatePermissionTypeCommand>
{
    public UpdatePermissionTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MenuId).GreaterThan(0);
    }
}

public class UpdatePermissionTypeCommandHandler(
    IApplicationDbContext context,
    IPermissionCache permissionCache) : IRequestHandler<UpdatePermissionTypeCommand, bool>
{
    public async Task<bool> Handle(UpdatePermissionTypeCommand request, CancellationToken ct)
    {
        var entity = await context.PermissionTypes.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (entity is null) return false;
        entity.Name = request.Name; entity.Code = request.Code; entity.Description = request.Description;
        entity.MenuId = request.MenuId; entity.IsActive = request.IsActive;
        entity.UpdatedDate = DateTime.UtcNow; entity.UpdatedBy = "system";
        await context.SaveChangesAsync(ct);

        // PermissionTypes are not directly scoped to a single tenant, so invalidate all.
        permissionCache.InvalidateAll();
        return true;
    }
}

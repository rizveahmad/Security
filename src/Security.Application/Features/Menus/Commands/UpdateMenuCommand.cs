using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Menus.Commands;

public record UpdateMenuCommand(int Id, string Name, string? Code, string? Url, string? Icon, int DisplayOrder, int ModuleId, bool IsActive) : IRequest<bool>;

public class UpdateMenuCommandValidator : AbstractValidator<UpdateMenuCommand>
{
    public UpdateMenuCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ModuleId).GreaterThan(0);
    }
}

public class UpdateMenuCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateMenuCommand, bool>
{
    public async Task<bool> Handle(UpdateMenuCommand request, CancellationToken ct)
    {
        var entity = await context.AppMenus.FirstOrDefaultAsync(m => m.Id == request.Id, ct);
        if (entity is null) return false;
        entity.Name = request.Name; entity.Code = request.Code; entity.Url = request.Url; entity.Icon = request.Icon;
        entity.DisplayOrder = request.DisplayOrder; entity.ModuleId = request.ModuleId; entity.IsActive = request.IsActive;
        entity.UpdatedDate = DateTime.UtcNow; entity.UpdatedBy = "system";
        await context.SaveChangesAsync(ct);
        return true;
    }
}

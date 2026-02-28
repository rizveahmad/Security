using FluentValidation;
using MediatR;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.Menus.Commands;

public record CreateMenuCommand(string Name, string? Code, string? Url, string? Icon, int DisplayOrder, int ModuleId, bool IsActive = true) : IRequest<int>;

public class CreateMenuCommandValidator : AbstractValidator<CreateMenuCommand>
{
    public CreateMenuCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ModuleId).GreaterThan(0);
    }
}

public class CreateMenuCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateMenuCommand, int>
{
    public async Task<int> Handle(CreateMenuCommand request, CancellationToken ct)
    {
        var entity = new AppMenu { Name = request.Name, Code = request.Code, Url = request.Url, Icon = request.Icon, DisplayOrder = request.DisplayOrder, ModuleId = request.ModuleId, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "system" };
        context.AppMenus.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity.Id;
    }
}

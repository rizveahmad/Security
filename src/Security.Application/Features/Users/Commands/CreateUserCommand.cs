using FluentValidation;
using MediatR;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.Users.Commands;

public record CreateUserCommand(string Email, string FirstName, string LastName, string Password, int RoleGroupId) : IRequest<string>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.RoleGroupId).GreaterThan(0).WithMessage("A Role Group must be assigned to the user.");
    }
}

public class CreateUserCommandHandler(IApplicationDbContext context, IUserCreationService userCreationService) : IRequestHandler<CreateUserCommand, string>
{
    public async Task<string> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var userId = await userCreationService.CreateUserAsync(request.Email, request.FirstName, request.LastName, request.Password, ct);
        var assignment = new UserRoleGroup { UserId = userId, RoleGroupId = request.RoleGroupId, CreatedDate = DateTime.UtcNow, CreatedBy = "system" };
        context.UserRoleGroups.Add(assignment);
        await context.SaveChangesAsync(ct);
        return userId;
    }
}

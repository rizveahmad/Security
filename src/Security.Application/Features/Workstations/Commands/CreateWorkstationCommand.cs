using FluentValidation;
using MediatR;
using Security.Application.Common.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.Features.Workstations.Commands;

public record CreateWorkstationCommand(string Name, string? Code, string? IPAddress, string? MACAddress, int CompanyId, bool IsActive = true) : IRequest<int>;

public class CreateWorkstationCommandValidator : AbstractValidator<CreateWorkstationCommand>
{
    public CreateWorkstationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public class CreateWorkstationCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateWorkstationCommand, int>
{
    public async Task<int> Handle(CreateWorkstationCommand request, CancellationToken ct)
    {
        var entity = new Workstation { Name = request.Name, Code = request.Code, IPAddress = request.IPAddress, MACAddress = request.MACAddress, CompanyId = request.CompanyId, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "system" };
        context.Workstations.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity.Id;
    }
}

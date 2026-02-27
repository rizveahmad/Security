using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;

namespace Security.Application.Features.Companies.Queries;

public record GetCompanyByIdQuery(int Id) : IRequest<CompanyDto?>;

public class GetCompanyByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetCompanyByIdQuery, CompanyDto?>
{
    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken ct)
        => await context.Companies.AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CompanyDto(c.Id, c.Name, c.Code, c.Address, c.IsActive))
            .FirstOrDefaultAsync(ct);
}

using MediatR;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;

namespace Security.Application.Features.Users.Queries;

public record GetUsersQuery(int PageNumber = 1, int PageSize = 10, string? Search = null, bool? IsActive = null)
    : IRequest<PaginatedList<UserDto>>;

public record UserDto(string Id, string? UserName, string? Email, string? FirstName, string? LastName, bool IsActive, int? RoleGroupId, string? RoleGroupName);

public class GetUsersQueryHandler(IUserQueryService userQueryService) : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    public Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
        => userQueryService.GetUsersAsync(request.PageNumber, request.PageSize, request.Search, request.IsActive, ct);
}

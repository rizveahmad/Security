using Security.Application.Common.Models;
using Security.Application.Features.Users.Queries;

namespace Security.Application.Common.Interfaces;

public interface IUserQueryService
{
    Task<PaginatedList<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default);
}

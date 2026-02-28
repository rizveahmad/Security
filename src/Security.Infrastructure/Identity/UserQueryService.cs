using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;
using Security.Application.Features.Users.Queries;
using Security.Infrastructure.Data;

namespace Security.Infrastructure.Identity;

public class UserQueryService(ApplicationDbContext context) : IUserQueryService
{
    public async Task<PaginatedList<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? search, bool? isActive = null, CancellationToken ct = default)
    {
        var query = from u in context.Users.AsNoTracking()
                    join urg in context.UserRoleGroups.AsNoTracking() on u.Id equals urg.UserId into urgs
                    from urg in urgs.Where(x => !x.IsDeleted).DefaultIfEmpty()
                    join rg in context.RoleGroups.AsNoTracking() on urg.RoleGroupId equals rg.Id into rgs
                    from rg in rgs.DefaultIfEmpty()
                    select new UserDto(u.Id, u.UserName, u.Email, u.FirstName, u.LastName, u.IsActive, (int?)urg.RoleGroupId, rg != null ? rg.Name : null);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => (u.Email != null && u.Email.Contains(search)) || (u.FirstName != null && u.FirstName.Contains(search)));

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.Email)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return new PaginatedList<UserDto>(items, total, pageNumber, pageSize);
    }
}

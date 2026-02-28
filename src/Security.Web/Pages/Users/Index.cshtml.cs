using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;
using Security.Infrastructure.Data;

namespace Security.Web.Pages.Users;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<User> Users { get; set; } = new();
    public string? Search { get; set; }
    public string? IsActiveFilter { get; set; }

    public async Task OnGetAsync(string? search, string? isActive)
    {
        Search = search;
        IsActiveFilter = isActive;

        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)));
        }

        if (isActive == "true")
            query = query.Where(u => u.IsActive);
        else if (isActive == "false")
            query = query.Where(u => !u.IsActive);

        Users = await query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
    }
}

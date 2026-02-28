using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;
using Security.Infrastructure.Data;

namespace Security.Web.Pages.Roles;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<Role> Roles { get; set; } = new();

    public async Task OnGetAsync()
    {
        Roles = await _db.Roles.OrderBy(r => r.Name).ToListAsync();
    }
}

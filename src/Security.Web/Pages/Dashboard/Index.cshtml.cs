using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;
using Security.Infrastructure.Data;

namespace Security.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalRoles { get; set; }
    public int RecentAuditCount { get; set; }
    public List<AuditLog> RecentAuditLogs { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalUsers = await _db.Users.CountAsync();
        ActiveUsers = await _db.Users.CountAsync(u => u.IsActive);
        TotalRoles = await _db.Roles.CountAsync();
        RecentAuditCount = await _db.AuditLogs.CountAsync(a => a.Timestamp >= DateTime.UtcNow.AddDays(-7));
        RecentAuditLogs = await _db.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToListAsync();
    }
}

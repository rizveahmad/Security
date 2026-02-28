using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;
using Security.Infrastructure.Data;

namespace Security.Web.Pages.Audit;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<AuditLog> Logs { get; set; } = new();
    public string? Search { get; set; }
    public string? ActionFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public bool HasNextPage { get; set; }
    private const int PageSize = 50;

    public async Task OnGetAsync(string? search, string? action, int page = 1)
    {
        Search = search;
        ActionFilter = action;
        CurrentPage = page;

        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.EntityName.Contains(search) || (a.UserName != null && a.UserName.Contains(search)));

        if (!string.IsNullOrWhiteSpace(action) && Enum.TryParse<AuditAction>(action, out var auditAction))
            query = query.Where(a => a.Action == auditAction);

        var total = await query.CountAsync();
        HasNextPage = total > page * PageSize;

        Logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Security.Application.Interfaces;
using Security.Infrastructure.Data;

namespace Security.Web.Pages.Users;

[Authorize]
public class ExportModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IExportService<UserExportDto> _exportService;

    public ExportModel(ApplicationDbContext db, IExportService<UserExportDto> exportService)
    {
        _db = db;
        _exportService = exportService;
    }

    public async Task<IActionResult> OnGetAsync(string? search, string? isActive)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search) || (u.Email != null && u.Email.Contains(search)));

        if (isActive == "true") query = query.Where(u => u.IsActive);
        else if (isActive == "false") query = query.Where(u => !u.IsActive);

        var users = await query.OrderBy(u => u.LastName).Select(u => new UserExportDto
        {
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email ?? string.Empty,
            IsActive = u.IsActive ? "Yes" : "No",
            CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd")
        }).ToListAsync();

        var bytes = await _exportService.ExportAsync(users, "Users");
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "users_export.xlsx");
    }
}

public class UserExportDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IsActive { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

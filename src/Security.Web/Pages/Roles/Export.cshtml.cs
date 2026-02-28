using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Security.Application.Interfaces;
using Security.Infrastructure.Data;

namespace Security.Web.Pages.Roles;

[Authorize]
public class ExportModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IExportService<RoleExportDto> _exportService;

    public ExportModel(ApplicationDbContext db, IExportService<RoleExportDto> exportService)
    {
        _db = db;
        _exportService = exportService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var roles = await _db.Roles.OrderBy(r => r.Name).Select(r => new RoleExportDto
        {
            Name = r.Name ?? string.Empty,
            Description = r.Description ?? string.Empty,
            IsActive = r.IsActive ? "Yes" : "No",
            CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd")
        }).ToListAsync();

        var bytes = await _exportService.ExportAsync(roles, "Roles");
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "roles_export.xlsx");
    }
}

public class RoleExportDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IsActive { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Security.Infrastructure.Data;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsers = await db.Users.CountAsync();
        ViewBag.TotalRoles = await db.AppRoles.CountAsync();
        ViewBag.TotalCompanies = await db.Companies.CountAsync();
        ViewBag.RecentAuditCount = await db.AuditLogs
            .CountAsync(a => a.Timestamp >= DateTime.UtcNow.AddDays(-7));
        return View();
    }
}

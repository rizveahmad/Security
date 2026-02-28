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

    /// <summary>
    /// Stores the selected tenant in the session so <see cref="Services.TenantContext"/>
    /// can resolve it on subsequent requests.
    /// SuperAdmin may pass an empty/null value to clear the filter (view all tenants).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SelectTenant(int? tenantId)
    {
        if (tenantId.HasValue)
            HttpContext.Session.SetInt32("SelectedTenantId", tenantId.Value);
        else
            HttpContext.Session.Remove("SelectedTenantId");

        var returnUrl = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }
}

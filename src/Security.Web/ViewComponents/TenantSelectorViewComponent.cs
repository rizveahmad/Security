using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Security.Application.Interfaces;
using Security.Infrastructure.Data;

namespace Security.Web.ViewComponents;

/// <summary>
/// Renders the tenant (company) selector dropdown shown in the admin topbar.
/// Loads all active companies and exposes the currently selected tenant ID.
/// </summary>
public class TenantSelectorViewComponent(ApplicationDbContext db, ITenantContext tenantContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var companies = await db.Companies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new TenantSelectorItem(c.Id, c.Name))
            .ToListAsync();

        var model = new TenantSelectorViewModel(companies, tenantContext.TenantId);
        return View(model);
    }
}

public record TenantSelectorItem(int Id, string Name);

public record TenantSelectorViewModel(
    IReadOnlyList<TenantSelectorItem> Companies,
    int? SelectedTenantId);

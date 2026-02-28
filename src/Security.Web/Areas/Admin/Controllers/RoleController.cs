using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Security.Application.Features.Companies.Queries;
using Security.Application.Features.Roles.Commands;
using Security.Application.Features.Roles.Queries;
using Security.Application.Interfaces;
using Security.Infrastructure.Data;
using Security.Web.Models.Admin;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class RoleController(IMediator mediator, ApplicationDbContext db, IExportService<RoleExportRow> exportService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null, int? companyId = null)
    {
        var result = await mediator.Send(new GetRolesQuery(page, 10, search, companyId));
        ViewBag.Search = search;
        ViewBag.CompanyId = companyId;
        ViewBag.Companies = await GetCompaniesSelectList();
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? companyId = null)
    {
        ViewBag.Companies = await GetCompaniesSelectList();
        ViewBag.PermissionTree = companyId.HasValue
            ? await mediator.Send(new GetPermissionTreeQuery(companyId.Value))
            : new List<PermissionTreeModuleDto>();
        return View(new CreateRoleCommand(string.Empty, string.Empty, null, companyId ?? 0, true, new List<int>()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleCommand command, int[] permissionTypeIds)
    {
        command = command with { PermissionTypeIds = permissionTypeIds.ToList() };
        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompaniesSelectList();
            ViewBag.PermissionTree = command.CompanyId > 0
                ? await mediator.Send(new GetPermissionTreeQuery(command.CompanyId))
                : new List<PermissionTreeModuleDto>();
            return View(command);
        }
        await mediator.Send(command);
        TempData["Success"] = "Role created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await mediator.Send(new GetRoleByIdQuery(id));
        if (dto is null) return NotFound();
        ViewBag.Companies = await GetCompaniesSelectList();
        ViewBag.PermissionTree = await mediator.Send(new GetPermissionTreeQuery(dto.CompanyId));
        ViewBag.SelectedPermissions = dto.PermissionTypeIds;
        return View(new UpdateRoleCommand(dto.Id, dto.Name, dto.Code, dto.Description, dto.CompanyId, dto.IsActive, dto.PermissionTypeIds));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateRoleCommand command, int[] permissionTypeIds)
    {
        command = command with { PermissionTypeIds = permissionTypeIds.ToList() };
        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompaniesSelectList();
            ViewBag.PermissionTree = await mediator.Send(new GetPermissionTreeQuery(command.CompanyId));
            ViewBag.SelectedPermissions = command.PermissionTypeIds;
            return View(command);
        }
        await mediator.Send(command);
        TempData["Success"] = "Role updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteRoleCommand(id));
        TempData["Success"] = "Role deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissionTree(int companyId)
    {
        var tree = await mediator.Send(new GetPermissionTreeQuery(companyId));
        return Json(tree);
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? search = null, int? companyId = null)
    {
        var query = db.AppRoles.AsNoTracking().Include(r => r.Company).AsQueryable();
        if (companyId.HasValue) query = query.Where(r => r.CompanyId == companyId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search) || r.Code.Contains(search));

        var roles = await query.OrderBy(r => r.Name).Select(r => new RoleExportRow
        {
            Name = r.Name,
            Code = r.Code,
            Description = r.Description ?? string.Empty,
            Company = r.Company != null ? r.Company.Name : string.Empty,
            IsActive = r.IsActive ? "Yes" : "No"
        }).ToListAsync();

        var bytes = await exportService.ExportAsync(roles, "Roles");
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "roles_export.xlsx");
    }

    private async Task<List<SelectListItem>> GetCompaniesSelectList()
    {
        var companies = await mediator.Send(new GetCompaniesQuery(1, 100));
        return companies.Items.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}

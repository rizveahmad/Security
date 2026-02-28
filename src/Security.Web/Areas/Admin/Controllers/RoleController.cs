using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Security.Application.Features.Companies.Queries;
using Security.Application.Features.Roles.Commands;
using Security.Application.Features.Roles.Queries;
using Security.Application.Interfaces;
using Security.Application.Models;
using Security.Domain.Entities;
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

    [HttpGet]
    public IActionResult Import() => View((ImportResult?)null);

    [HttpGet]
    public IActionResult DownloadRoleTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Roles");
        ws.Cell(1, 1).Value = "Name*";
        ws.Cell(1, 2).Value = "Code*";
        ws.Cell(1, 3).Value = "Description";
        ws.Cell(1, 4).Value = "Company ID*";
        ws.Cell(1, 5).Value = "Is Active*";
        ws.Row(1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = "Admin";
        ws.Cell(2, 2).Value = "ADMIN";
        ws.Cell(2, 3).Value = "Administrator role";
        ws.Cell(2, 4).Value = 1;
        ws.Cell(2, 5).Value = "Yes";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "roles_import_template.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a file to import.");
            return View((ImportResult?)null);
        }

        var result = new ImportResult();
        using var stream = file.OpenReadStream();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        var headers = new List<string>();
        int col = 1;
        while (!ws.Row(1).Cell(col).IsEmpty())
        {
            headers.Add(ws.Row(1).Cell(col).GetString().Replace("*", "").Trim().ToLowerInvariant());
            col++;
        }

        var requiredHeaders = new[] { "name", "code", "company id" };
        var missingHeaders = requiredHeaders.Where(h => !headers.Contains(h)).ToList();
        if (missingHeaders.Any())
        {
            ModelState.AddModelError(string.Empty, $"Missing required column(s): {string.Join(", ", missingHeaders)}. Please use the template.");
            return View((ImportResult?)null);
        }

        int row = 2;
        var rolesToAdd = new List<AppRole>();
        while (!ws.Row(row).IsEmpty())
        {
            var name = ws.Row(row).Cell(headers.IndexOf("name") + 1).GetString().Trim();
            var code = ws.Row(row).Cell(headers.IndexOf("code") + 1).GetString().Trim();
            var description = ws.Row(row).Cell(headers.IndexOf("description") + 1).GetString().Trim();
            var companyIdStr = ws.Row(row).Cell(headers.IndexOf("company id") + 1).GetString().Trim();
            var isActiveStr = headers.Contains("is active")
                ? ws.Row(row).Cell(headers.IndexOf("is active") + 1).GetString().Trim()
                : "Yes";

            if (string.IsNullOrWhiteSpace(name))
            {
                result.RowErrors.Add(new RowError { RowNumber = row, Field = "Name", Error = "Name is required." });
                result.ErrorCount++;
                row++;
                continue;
            }

            if (!int.TryParse(companyIdStr, out var companyId) || companyId <= 0)
            {
                result.RowErrors.Add(new RowError { RowNumber = row, Field = "Company ID", Error = "A valid Company ID is required." });
                result.ErrorCount++;
                row++;
                continue;
            }

            var isActive = !isActiveStr.Equals("No", StringComparison.OrdinalIgnoreCase);

            rolesToAdd.Add(new AppRole
            {
                Name = name,
                Code = string.IsNullOrWhiteSpace(code) ? name.ToUpperInvariant() : code,
                Description = description,
                CompanyId = companyId,
                IsActive = isActive
            });

            result.SuccessCount++;
            row++;
        }

        if (rolesToAdd.Any())
        {
            db.AppRoles.AddRange(rolesToAdd);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // If bulk save fails, report all as failed
                foreach (var addedRole in rolesToAdd)
                {
                    result.RowErrors.Add(new RowError { RowNumber = 0, Field = "Role", Error = ex.Message });
                }
                result.ErrorCount += rolesToAdd.Count;
                result.SuccessCount = 0;
            }
        }

        return View(result);
    }

    private async Task<List<SelectListItem>> GetCompaniesSelectList()
    {
        var companies = await mediator.Send(new GetCompaniesQuery(1, 100));
        return companies.Items.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}

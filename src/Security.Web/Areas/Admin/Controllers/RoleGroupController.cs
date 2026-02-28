using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Security.Application.Features.Companies.Queries;
using Security.Application.Features.RoleGroups.Commands;
using Security.Application.Features.RoleGroups.Queries;
using Security.Application.Features.Roles.Queries;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class RoleGroupController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null, int? companyId = null)
    {
        var result = await mediator.Send(new GetRoleGroupsQuery(page, 10, search, companyId));
        ViewBag.Search = search;
        ViewBag.Companies = await GetCompaniesSelectList();
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Companies = await GetCompaniesSelectList();
        ViewBag.Roles = await GetRolesSelectList();
        return View(new CreateRoleGroupCommand(string.Empty, null, null, 0, true, new List<int>()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleGroupCommand command, int[] roleIds)
    {
        command = command with { RoleIds = roleIds.ToList() };
        if (!ModelState.IsValid) { ViewBag.Companies = await GetCompaniesSelectList(); ViewBag.Roles = await GetRolesSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Role Group created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await mediator.Send(new GetRoleGroupByIdQuery(id));
        if (dto is null) return NotFound();
        ViewBag.Companies = await GetCompaniesSelectList();
        ViewBag.Roles = await GetRolesSelectList();
        ViewBag.SelectedRoles = dto.RoleIds;
        return View(new UpdateRoleGroupCommand(dto.Id, dto.Name, dto.Code, dto.Description, dto.CompanyId, dto.IsActive, dto.RoleIds));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateRoleGroupCommand command, int[] roleIds)
    {
        command = command with { RoleIds = roleIds.ToList() };
        if (!ModelState.IsValid) { ViewBag.Companies = await GetCompaniesSelectList(); ViewBag.Roles = await GetRolesSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Role Group updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteRoleGroupCommand(id));
        TempData["Success"] = "Role Group deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetCompaniesSelectList()
    {
        var companies = await mediator.Send(new GetCompaniesQuery(1, 100));
        return companies.Items.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }

    private async Task<List<SelectListItem>> GetRolesSelectList()
    {
        var roles = await mediator.Send(new GetRolesQuery(1, 200));
        return roles.Items.Select(r => new SelectListItem(r.Name, r.Id.ToString())).ToList();
    }
}

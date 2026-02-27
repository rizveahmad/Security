using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Security.Application.Features.Companies.Queries;
using Security.Application.Features.Modules.Commands;
using Security.Application.Features.Modules.Queries;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ModuleController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null, int? companyId = null)
    {
        var result = await mediator.Send(new GetModulesQuery(page, 10, search, companyId));
        ViewBag.Search = search;
        ViewBag.CompanyId = companyId;
        ViewBag.Companies = await GetCompaniesSelectList();
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Companies = await GetCompaniesSelectList();
        return View(new CreateModuleCommand(string.Empty, null, null, 0));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateModuleCommand command)
    {
        if (!ModelState.IsValid) { ViewBag.Companies = await GetCompaniesSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Module created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await mediator.Send(new GetModuleByIdQuery(id));
        if (dto is null) return NotFound();
        ViewBag.Companies = await GetCompaniesSelectList();
        return View(new UpdateModuleCommand(dto.Id, dto.Name, dto.Code, dto.Description, dto.CompanyId, dto.IsActive));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateModuleCommand command)
    {
        if (!ModelState.IsValid) { ViewBag.Companies = await GetCompaniesSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Module updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteModuleCommand(id));
        TempData["Success"] = "Module deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetCompaniesSelectList()
    {
        var companies = await mediator.Send(new GetCompaniesQuery(1, 100));
        return companies.Items.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}

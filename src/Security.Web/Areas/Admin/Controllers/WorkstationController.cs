using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Security.Application.Features.Companies.Queries;
using Security.Application.Features.Workstations.Commands;
using Security.Application.Features.Workstations.Queries;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class WorkstationController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null, int? companyId = null)
    {
        var result = await mediator.Send(new GetWorkstationsQuery(page, 10, search, companyId));
        ViewBag.Search = search;
        ViewBag.Companies = await GetCompaniesSelectList();
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Companies = await GetCompaniesSelectList();
        return View(new CreateWorkstationCommand(string.Empty, null, null, null, 0));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkstationCommand command)
    {
        if (!ModelState.IsValid) { ViewBag.Companies = await GetCompaniesSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Workstation created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await mediator.Send(new GetWorkstationByIdQuery(id));
        if (dto is null) return NotFound();
        ViewBag.Companies = await GetCompaniesSelectList();
        return View(new UpdateWorkstationCommand(dto.Id, dto.Name, dto.Code, dto.IPAddress, dto.MACAddress, dto.CompanyId, dto.IsActive));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateWorkstationCommand command)
    {
        if (!ModelState.IsValid) { ViewBag.Companies = await GetCompaniesSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Workstation updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteWorkstationCommand(id));
        TempData["Success"] = "Workstation deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetCompaniesSelectList()
    {
        var companies = await mediator.Send(new GetCompaniesQuery(1, 100));
        return companies.Items.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}

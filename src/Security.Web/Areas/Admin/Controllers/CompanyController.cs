using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Application.Features.Companies.Commands;
using Security.Application.Features.Companies.Queries;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class CompanyController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null)
    {
        var result = await mediator.Send(new GetCompaniesQuery(page, 10, search));
        ViewBag.Search = search;
        return View(result);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateCompanyCommand(string.Empty, null, null));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCompanyCommand command)
    {
        if (!ModelState.IsValid) return View(command);
        await mediator.Send(command);
        TempData["Success"] = "Company created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await mediator.Send(new GetCompanyByIdQuery(id));
        if (dto is null) return NotFound();
        return View(new UpdateCompanyCommand(dto.Id, dto.Name, dto.Code, dto.Address, dto.IsActive));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateCompanyCommand command)
    {
        if (!ModelState.IsValid) return View(command);
        await mediator.Send(command);
        TempData["Success"] = "Company updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteCompanyCommand(id));
        TempData["Success"] = "Company deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

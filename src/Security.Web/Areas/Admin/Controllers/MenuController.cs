using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Security.Application.Features.Menus.Commands;
using Security.Application.Features.Menus.Queries;
using Security.Application.Features.Modules.Queries;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class MenuController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null, int? moduleId = null)
    {
        var result = await mediator.Send(new GetMenusQuery(page, 10, search, moduleId));
        ViewBag.Search = search;
        ViewBag.ModuleId = moduleId;
        ViewBag.Modules = await GetModulesSelectList();
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Modules = await GetModulesSelectList();
        return View(new CreateMenuCommand(string.Empty, null, null, null, 0, 0));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMenuCommand command)
    {
        if (!ModelState.IsValid) { ViewBag.Modules = await GetModulesSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Menu created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await mediator.Send(new GetMenuByIdQuery(id));
        if (dto is null) return NotFound();
        ViewBag.Modules = await GetModulesSelectList();
        return View(new UpdateMenuCommand(dto.Id, dto.Name, dto.Code, dto.Url, dto.Icon, dto.DisplayOrder, dto.ModuleId, dto.IsActive));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateMenuCommand command)
    {
        if (!ModelState.IsValid) { ViewBag.Modules = await GetModulesSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Menu updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteMenuCommand(id));
        TempData["Success"] = "Menu deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetModulesSelectList()
    {
        var modules = await mediator.Send(new GetModulesQuery(1, 100));
        return modules.Items.Select(m => new SelectListItem(m.Name, m.Id.ToString())).ToList();
    }
}

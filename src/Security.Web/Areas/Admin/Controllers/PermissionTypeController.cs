using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Security.Application.Features.Menus.Queries;
using Security.Application.Features.PermissionTypes.Commands;
using Security.Application.Features.PermissionTypes.Queries;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class PermissionTypeController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null, int? menuId = null)
    {
        var result = await mediator.Send(new GetPermissionTypesQuery(page, 10, search, menuId));
        ViewBag.Search = search;
        ViewBag.MenuId = menuId;
        ViewBag.Menus = await GetMenusSelectList();
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Menus = await GetMenusSelectList();
        return View(new CreatePermissionTypeCommand(string.Empty, null, null, 0));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePermissionTypeCommand command)
    {
        if (!ModelState.IsValid) { ViewBag.Menus = await GetMenusSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Permission Type created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await mediator.Send(new GetPermissionTypeByIdQuery(id));
        if (dto is null) return NotFound();
        ViewBag.Menus = await GetMenusSelectList();
        return View(new UpdatePermissionTypeCommand(dto.Id, dto.Name, dto.Code, dto.Description, dto.MenuId, dto.IsActive));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdatePermissionTypeCommand command)
    {
        if (!ModelState.IsValid) { ViewBag.Menus = await GetMenusSelectList(); return View(command); }
        await mediator.Send(command);
        TempData["Success"] = "Permission Type updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeletePermissionTypeCommand(id));
        TempData["Success"] = "Permission Type deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetMenusSelectList()
    {
        var menus = await mediator.Send(new GetMenusQuery(1, 200));
        return menus.Items.Select(m => new SelectListItem(m.Name, m.Id.ToString())).ToList();
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Security.Application.Features.RoleGroups.Queries;
using Security.Application.Features.Users.Commands;
using Security.Application.Features.Users.Queries;
using Security.Web.Models.Admin;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class UserController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null)
    {
        var result = await mediator.Send(new GetUsersQuery(page, 10, search));
        ViewBag.Search = search;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.RoleGroups = await GetRoleGroupsSelectList();
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel vm)
    {
        if (!ModelState.IsValid) { ViewBag.RoleGroups = await GetRoleGroupsSelectList(); return View(vm); }
        await mediator.Send(new CreateUserCommand(vm.Email, vm.FirstName, vm.LastName, vm.Password, vm.RoleGroupId));
        TempData["Success"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AssignRoleGroup(string id)
    {
        ViewBag.RoleGroups = await GetRoleGroupsSelectList();
        return View(new AssignRoleGroupViewModel { UserId = id });
    }

    [HttpPost]
    public async Task<IActionResult> AssignRoleGroup(AssignRoleGroupViewModel vm)
    {
        if (!ModelState.IsValid) { ViewBag.RoleGroups = await GetRoleGroupsSelectList(); return View(vm); }
        await mediator.Send(new AssignRoleGroupCommand(vm.UserId, vm.RoleGroupId));
        TempData["Success"] = "Role Group assigned successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetRoleGroupsSelectList()
    {
        var rgs = await mediator.Send(new GetRoleGroupsQuery(1, 200));
        return rgs.Items.Select(rg => new SelectListItem(rg.Name, rg.Id.ToString())).ToList();
    }
}

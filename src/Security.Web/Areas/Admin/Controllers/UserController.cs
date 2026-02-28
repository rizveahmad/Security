using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Security.Application.Features.RoleGroups.Queries;
using Security.Application.Features.Users.Commands;
using Security.Application.Features.Users.Queries;
using Security.Application.Interfaces;
using Security.Infrastructure.Data;
using Security.Infrastructure.Identity;
using Security.Web.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class UserController(IMediator mediator, UserManager<ApplicationUser> userManager, ApplicationDbContext db, IExportService<UserExportRow> exportService) : Controller
{    [HttpGet]
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
    public async Task<IActionResult> Edit(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var vm = new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await userManager.FindByIdAsync(vm.Id);
        if (user is null) return NotFound();
        user.FirstName = vm.FirstName;
        user.LastName = vm.LastName;
        user.IsActive = vm.IsActive;
        user.UpdatedBy = User.Identity?.Name ?? "system";
        user.UpdatedDate = DateTime.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }
        TempData["Success"] = "User updated successfully.";
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

    [HttpGet]
    public async Task<IActionResult> Export(string? search = null)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => (u.FirstName != null && u.FirstName.Contains(search))
                || (u.LastName != null && u.LastName.Contains(search))
                || (u.Email != null && u.Email.Contains(search)));

        var users = await query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new UserExportRow
            {
                FirstName = u.FirstName ?? string.Empty,
                LastName = u.LastName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                IsActive = u.IsActive ? "Yes" : "No"
            }).ToListAsync();

        var bytes = await exportService.ExportAsync(users, "Users");
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "users_export.xlsx");
    }

    private async Task<List<SelectListItem>> GetRoleGroupsSelectList()
    {
        var rgs = await mediator.Send(new GetRoleGroupsQuery(1, 200));
        return rgs.Items.Select(rg => new SelectListItem(rg.Name, rg.Id.ToString())).ToList();
    }
}

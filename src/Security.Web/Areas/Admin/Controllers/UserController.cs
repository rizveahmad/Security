using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Security.Application.Features.RoleGroups.Queries;
using Security.Application.Features.Users.Commands;
using Security.Application.Features.Users.Queries;
using Security.Application.Interfaces;
using Security.Application.Models;
using Security.Infrastructure.Data;
using Security.Infrastructure.Identity;
using Security.Web.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class UserController(IMediator mediator, UserManager<ApplicationUser> userManager, ApplicationDbContext db, IExportService<UserExportRow> exportService) : Controller
{    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null, bool? isActive = null)
    {
        var result = await mediator.Send(new GetUsersQuery(page, 10, search, isActive));
        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
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
    public async Task<IActionResult> Export(string? search = null, bool? isActive = null)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => (u.FirstName != null && u.FirstName.Contains(search))
                || (u.LastName != null && u.LastName.Contains(search))
                || (u.Email != null && u.Email.Contains(search)));

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

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

    [HttpGet]
    public IActionResult Import() => View((ImportResult?)null);

    [HttpGet]
    public IActionResult DownloadUserTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Users");
        ws.Cell(1, 1).Value = "Email*";
        ws.Cell(1, 2).Value = "First Name*";
        ws.Cell(1, 3).Value = "Last Name*";
        ws.Cell(1, 4).Value = "Password*";
        ws.Row(1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = "user@example.com";
        ws.Cell(2, 2).Value = "John";
        ws.Cell(2, 3).Value = "Doe";
        ws.Cell(2, 4).Value = "Password123!";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "users_import_template.xlsx");
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

        var requiredHeaders = new[] { "email", "first name", "last name", "password" };
        var missingHeaders = requiredHeaders.Where(h => !headers.Contains(h)).ToList();
        if (missingHeaders.Any())
        {
            ModelState.AddModelError(string.Empty, $"Missing required column(s): {string.Join(", ", missingHeaders)}. Please use the template.");
            return View((ImportResult?)null);
        }

        int row = 2;
        while (!ws.Row(row).IsEmpty())
        {
            var email = ws.Row(row).Cell(headers.IndexOf("email") + 1).GetString().Trim();
            var firstName = ws.Row(row).Cell(headers.IndexOf("first name") + 1).GetString().Trim();
            var lastName = ws.Row(row).Cell(headers.IndexOf("last name") + 1).GetString().Trim();
            var password = ws.Row(row).Cell(headers.IndexOf("password") + 1).GetString().Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                result.RowErrors.Add(new RowError { RowNumber = row, Field = "Email", Error = "Email is required." });
                result.ErrorCount++;
                row++;
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                CreatedBy = User.Identity?.Name ?? "import",
                CreatedDate = DateTime.UtcNow
            };
            var identityResult = await userManager.CreateAsync(user, password);
            if (identityResult.Succeeded)
            {
                result.SuccessCount++;
            }
            else
            {
                result.RowErrors.Add(new RowError { RowNumber = row, Field = "User", Error = string.Join("; ", identityResult.Errors.Select(e => e.Description)) });
                result.ErrorCount++;
            }
            row++;
        }

        return View(result);
    }

    private async Task<List<SelectListItem>> GetRoleGroupsSelectList()
    {
        var rgs = await mediator.Send(new GetRoleGroupsQuery(1, 200));
        return rgs.Items.Select(rg => new SelectListItem(rg.Name, rg.Id.ToString())).ToList();
    }
}

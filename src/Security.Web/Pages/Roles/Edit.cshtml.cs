using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Security.Web.Pages.Roles;

[Authorize]
public class EditModel : PageModel
{
    private readonly RoleManager<Role> _roleManager;
    private readonly IAuditService _auditService;

    public EditModel(RoleManager<Role> roleManager, IAuditService auditService)
    {
        _roleManager = roleManager;
        _auditService = auditService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public string RowVersion { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null) return NotFound();

        Input = new InputModel
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsActive = role.IsActive,
            RowVersion = Convert.ToBase64String(role.RowVersion)
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var role = await _roleManager.FindByIdAsync(Input.Id.ToString());
        if (role is null) return NotFound();

        // Optimistic concurrency: verify RowVersion has not changed since the form was loaded
        if (!string.IsNullOrEmpty(Input.RowVersion))
        {
            var formRowVersion = Convert.FromBase64String(Input.RowVersion);
            if (!role.RowVersion.SequenceEqual(formRowVersion))
            {
                ModelState.AddModelError(string.Empty, "The record was modified by another user. Please reload and try again.");
                return Page();
            }
        }

        var oldValues = JsonSerializer.Serialize(new { role.Name, role.Description, role.IsActive });

        role.Name = Input.Name;
        role.Description = Input.Description;
        role.IsActive = Input.IsActive;
        role.RowVersion = Guid.NewGuid().ToByteArray();

        try
        {
            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                await _auditService.LogAsync("Role", AuditAction.Update, role.Id.ToString(),
                    oldValues: oldValues,
                    newValues: JsonSerializer.Serialize(new { role.Name, role.Description, role.IsActive }));
                TempData["SuccessMessage"] = "Role updated successfully.";
                return RedirectToPage("/Roles/Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "The record was modified by another user. Please reload and try again.");
        }

        return Page();
    }
}

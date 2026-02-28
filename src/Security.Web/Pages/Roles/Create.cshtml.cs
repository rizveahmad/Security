using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Security.Web.Pages.Roles;

[Authorize]
public class CreateModel : PageModel
{
    private readonly RoleManager<Role> _roleManager;
    private readonly IAuditService _auditService;

    public CreateModel(RoleManager<Role> roleManager, IAuditService auditService)
    {
        _roleManager = roleManager;
        _auditService = auditService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var role = new Role
        {
            Name = Input.Name,
            Description = Input.Description,
            IsActive = Input.IsActive,
            CreatedAt = DateTime.UtcNow,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        var result = await _roleManager.CreateAsync(role);

        if (result.Succeeded)
        {
            await _auditService.LogAsync("Role", AuditAction.Create, role.Id.ToString(),
                newValues: JsonSerializer.Serialize(new { role.Name, role.Description }));
            TempData["SuccessMessage"] = "Role created successfully.";
            return RedirectToPage("/Roles/Index");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Security.Web.Pages.Users;

[Authorize]
public class EditModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IAuditService _auditService;

    public EditModel(UserManager<User> userManager, IAuditService auditService)
    {
        _userManager = userManager;
        _auditService = auditService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public string RowVersion { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        Input = new InputModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            RowVersion = Convert.ToBase64String(user.RowVersion)
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.FindByIdAsync(Input.Id.ToString());
        if (user is null) return NotFound();

        // Optimistic concurrency: verify RowVersion has not changed since the form was loaded
        if (!string.IsNullOrEmpty(Input.RowVersion))
        {
            var formRowVersion = Convert.FromBase64String(Input.RowVersion);
            if (!user.RowVersion.SequenceEqual(formRowVersion))
            {
                ModelState.AddModelError(string.Empty, "The record was modified by another user. Please reload and try again.");
                return Page();
            }
        }

        var oldValues = JsonSerializer.Serialize(new { user.FirstName, user.LastName, user.IsActive });

        user.FirstName = Input.FirstName;
        user.LastName = Input.LastName;
        user.IsActive = Input.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.RowVersion = Guid.NewGuid().ToByteArray();

        try
        {
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _auditService.LogAsync("User", AuditAction.Update, user.Id.ToString(),
                    oldValues: oldValues,
                    newValues: JsonSerializer.Serialize(new { user.FirstName, user.LastName, user.IsActive }));
                TempData["SuccessMessage"] = "User updated successfully.";
                return RedirectToPage("/Users/Index");
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

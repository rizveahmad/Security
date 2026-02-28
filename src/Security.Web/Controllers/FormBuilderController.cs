using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Application.Interfaces;
using Security.Domain.Entities;

namespace Security.Web.Controllers;

[Authorize(Roles = "SuperAdmin,Admin")]
public class FormBuilderController : Controller
{
    private readonly IFormBuilderService _svc;
    public FormBuilderController(IFormBuilderService svc) => _svc = svc;

    public async Task<IActionResult> Designer(string menuKey)
    {
        ViewBag.MenuKey = menuKey;
        var def = await _svc.GetByMenuKeyAsync(menuKey);
        ViewBag.FormName = def?.Name ?? menuKey;
        // Re-serialize through System.Text.Json so < > & are escaped as \u003C \u003E \u0026
        // preventing HTML/script-injection when the value is embedded in a <script> block.
        var rawJson = def?.FieldsJson ?? "[]";
        var parsed = JsonSerializer.Deserialize<JsonElement>(rawJson);
        ViewBag.FieldsJson = JsonSerializer.Serialize(parsed);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromForm] string menuKey, [FromForm] string name, [FromForm] string fieldsJson)
    {
        await _svc.UpsertAsync(menuKey, name, fieldsJson);
        TempData["Success"] = $"Form definition for '{menuKey}' saved.";
        return RedirectToAction(nameof(Designer), new { menuKey });
    }
}

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Application.Interfaces;

namespace Security.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly IFormBuilderService _formBuilder;
    private const string MenuKey = "customers";

    public CustomersController(IFormBuilderService formBuilder) => _formBuilder = formBuilder;

    public IActionResult Index() => View();

    public async Task<IActionResult> Create()
    {
        var fields = await _formBuilder.GetFieldsAsync(MenuKey);
        ViewBag.ExtensionFields = fields;
        ViewBag.MenuKey = MenuKey;
        ViewBag.ExtensionValues = new Dictionary<string, string?>();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] string name, [FromForm] string email,
        [FromForm] IFormCollection form)
    {
        if (string.IsNullOrWhiteSpace(name))
            ModelState.AddModelError("name", "Name is required.");

        var extValues = ExtractExtensionValues(form);
        var validation = await _formBuilder.ValidateAsync(MenuKey, extValues);
        foreach (var err in validation.Errors)
            ModelState.AddModelError($"ext_{err.Key}", err.Value);

        if (!ModelState.IsValid)
        {
            var fields = await _formBuilder.GetFieldsAsync(MenuKey);
            ViewBag.ExtensionFields = fields;
            ViewBag.MenuKey = MenuKey;
            ViewBag.ExtensionValues = extValues;
            return View();
        }

        var recordKey = Guid.NewGuid().ToString();
        if (extValues.Count > 0)
            await _formBuilder.SaveSubmissionAsync(MenuKey, recordKey, extValues);

        TempData["Success"] = $"Customer '{name}' created.";
        return RedirectToAction(nameof(Index));
    }

    private static Dictionary<string, string?> ExtractExtensionValues(IFormCollection form)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("ext_"))
                dict[key[4..]] = form[key].FirstOrDefault();
        }
        return dict;
    }
}

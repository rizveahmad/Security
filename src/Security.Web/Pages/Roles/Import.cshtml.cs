using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Security.Application.Interfaces;
using Security.Application.Models;
using Security.Domain.Entities;

namespace Security.Web.Pages.Roles;

[Authorize]
public class ImportModel : PageModel
{
    private readonly RoleManager<Role> _roleManager;
    private readonly IAuditService _auditService;

    public ImportModel(RoleManager<Role> roleManager, IAuditService auditService)
    {
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public ImportResult? ImportResult { get; set; }

    public void OnGet() { }

    public IActionResult OnGetDownloadTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Roles");

        var headers = new[] { "Name*", "Description", "IsActive" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            cell.Style.Font.FontColor = XLColor.White;
        }

        ws.Cell(2, 1).Value = "Manager";
        ws.Cell(2, 2).Value = "Manager role";
        ws.Cell(2, 3).Value = "Yes";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "roles_template.xlsx");
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please upload a file.");
            return Page();
        }

        var result = new ImportResult();

        using var stream = file.OpenReadStream();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        var headers = new List<string>();
        int col = 1;
        while (!ws.Row(1).Cell(col).IsEmpty())
        {
            headers.Add(ws.Row(1).Cell(col).GetString().Replace("*", "").Trim());
            col++;
        }

        int rowNum = 2;
        while (!ws.Row(rowNum).IsEmpty())
        {
            var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < headers.Count; c++)
                rowData[headers[c]] = ws.Row(rowNum).Cell(c + 1).GetString().Trim();

            var name = rowData.GetValueOrDefault("Name", "");
            var description = rowData.GetValueOrDefault("Description", "");
            var isActiveStr = rowData.GetValueOrDefault("IsActive", "Yes");

            if (string.IsNullOrWhiteSpace(name))
            {
                result.RowErrors.Add(new RowError { RowNumber = rowNum, Field = "Name", Error = "Role Name is required." });
                result.ErrorCount++;
            }
            else if (await _roleManager.RoleExistsAsync(name))
            {
                result.RowErrors.Add(new RowError { RowNumber = rowNum, Field = "Name", Error = "Role already exists." });
                result.ErrorCount++;
            }
            else
            {
                var isActive = !isActiveStr.Equals("No", StringComparison.OrdinalIgnoreCase);
                var role = new Role
                {
                    Name = name,
                    Description = description,
                    IsActive = isActive,
                    CreatedAt = DateTime.UtcNow,
                    RowVersion = Guid.NewGuid().ToByteArray()
                };

                var createResult = await _roleManager.CreateAsync(role);
                if (createResult.Succeeded)
                {
                    await _auditService.LogAsync("Role", AuditAction.Import, role.Id.ToString(),
                        newValues: $"{{\"Name\":\"{name}\"}}");
                    result.SuccessCount++;
                }
                else
                {
                    foreach (var err in createResult.Errors)
                        result.RowErrors.Add(new RowError { RowNumber = rowNum, Field = "Role", Error = err.Description });
                    result.ErrorCount++;
                }
            }

            rowNum++;
        }

        ImportResult = result;
        return Page();
    }
}

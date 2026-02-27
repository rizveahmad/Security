using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Security.Application.Interfaces;
using Security.Application.Models;
using Security.Domain.Entities;
using System.Text.RegularExpressions;

namespace Security.Web.Pages.Users;

[Authorize]
public class ImportModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IAuditService _auditService;

    public ImportModel(UserManager<User> userManager, IAuditService auditService)
    {
        _userManager = userManager;
        _auditService = auditService;
    }

    public ImportResult? ImportResult { get; set; }

    public void OnGet() { }

    public IActionResult OnGetDownloadTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Users");

        var headers = new[] { "FirstName*", "LastName*", "Email*", "IsActive" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            cell.Style.Font.FontColor = XLColor.White;
        }

        ws.Cell(2, 1).Value = "John";
        ws.Cell(2, 2).Value = "Doe";
        ws.Cell(2, 3).Value = "john.doe@example.com";
        ws.Cell(2, 4).Value = "Yes";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "users_template.xlsx");
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

            var rowErrors = new List<RowError>();

            var firstName = rowData.GetValueOrDefault("FirstName", "");
            var lastName = rowData.GetValueOrDefault("LastName", "");
            var email = rowData.GetValueOrDefault("Email", "");
            var isActiveStr = rowData.GetValueOrDefault("IsActive", "Yes");

            if (string.IsNullOrWhiteSpace(firstName))
                rowErrors.Add(new RowError { RowNumber = rowNum, Field = "FirstName", Error = "First Name is required." });

            if (string.IsNullOrWhiteSpace(lastName))
                rowErrors.Add(new RowError { RowNumber = rowNum, Field = "LastName", Error = "Last Name is required." });

            if (string.IsNullOrWhiteSpace(email))
                rowErrors.Add(new RowError { RowNumber = rowNum, Field = "Email", Error = "Email is required." });
            else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                rowErrors.Add(new RowError { RowNumber = rowNum, Field = "Email", Error = "Email format is invalid." });
            else if (await _userManager.FindByEmailAsync(email) is not null)
                rowErrors.Add(new RowError { RowNumber = rowNum, Field = "Email", Error = "Email already exists." });

            if (rowErrors.Any())
            {
                result.RowErrors.AddRange(rowErrors);
                result.ErrorCount++;
            }
            else
            {
                var isActive = !isActiveStr.Equals("No", StringComparison.OrdinalIgnoreCase);
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    IsActive = isActive,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    RowVersion = Guid.NewGuid().ToByteArray()
                };

                var createResult = await _userManager.CreateAsync(user, "Temp@123456!");
                if (createResult.Succeeded)
                {
                    await _auditService.LogAsync("User", AuditAction.Import, user.Id.ToString(),
                        newValues: $"{{\"Email\":\"{email}\"}}");
                    result.SuccessCount++;
                }
                else
                {
                    foreach (var err in createResult.Errors)
                        result.RowErrors.Add(new RowError { RowNumber = rowNum, Field = "User", Error = err.Description });
                    result.ErrorCount++;
                }
            }

            rowNum++;
        }

        ImportResult = result;
        return Page();
    }
}

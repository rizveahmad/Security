using ClosedXML.Excel;
using Security.Application.Interfaces;
using Security.Application.Models;

namespace Security.Infrastructure.Services;

public class ExcelImportService : IImportService
{
    public Task<ImportResult> ImportAsync(Stream fileStream, string fileName)
    {
        var result = new ImportResult();
        return Task.FromResult(result);
    }

    public static Dictionary<string, string> ReadRow(IXLRow row, IList<string> headers)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
        {
            dict[headers[i]] = row.Cell(i + 1).GetString().Trim();
        }
        return dict;
    }

    public static List<string> ReadHeaders(IXLWorksheet ws)
    {
        var headers = new List<string>();
        var headerRow = ws.Row(1);
        int col = 1;
        while (!headerRow.Cell(col).IsEmpty())
        {
            headers.Add(headerRow.Cell(col).GetString().Replace("*", "").Trim());
            col++;
        }
        return headers;
    }
}

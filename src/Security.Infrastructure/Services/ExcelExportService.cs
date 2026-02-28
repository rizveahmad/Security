using ClosedXML.Excel;
using Security.Application.Interfaces;
using System.Reflection;

namespace Security.Infrastructure.Services;

public class ExcelExportService<T> : IExportService<T>
{
    public Task<byte[]> ExportAsync(IEnumerable<T> data, string sheetName = "Export")
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        for (int i = 0; i < properties.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = properties[i].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var dataList = data.ToList();
        for (int row = 0; row < dataList.Count; row++)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(dataList[row]);
                var cell = ws.Cell(row + 2, col + 1);
                if (value is DateTime dt)
                    cell.Value = dt.ToString("yyyy-MM-dd HH:mm:ss");
                else if (value is bool b)
                    cell.Value = b ? "Yes" : "No";
                else
                    cell.Value = value?.ToString() ?? string.Empty;
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}

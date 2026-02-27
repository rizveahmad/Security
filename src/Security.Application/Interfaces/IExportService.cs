namespace Security.Application.Interfaces;

public interface IExportService<T>
{
    Task<byte[]> ExportAsync(IEnumerable<T> data, string sheetName = "Export");
}

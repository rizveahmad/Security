using Security.Application.Models;

namespace Security.Application.Interfaces;

public interface IImportService
{
    Task<ImportResult> ImportAsync(Stream fileStream, string fileName);
}

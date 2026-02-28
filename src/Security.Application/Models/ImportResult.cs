namespace Security.Application.Models;

public class ImportResult
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<RowError> RowErrors { get; set; } = new();
    public bool HasErrors => RowErrors.Any();
}

public class RowError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

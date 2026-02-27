namespace Security.Application.Models;

public class ExportRequest
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

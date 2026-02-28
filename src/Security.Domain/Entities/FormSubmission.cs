namespace Security.Domain.Entities;

public class FormSubmission
{
    public int Id { get; set; }
    public string MenuKey { get; set; } = string.Empty;
    public string RecordKey { get; set; } = string.Empty;
    public int FormDefinitionId { get; set; }
    public string ValuesJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public FormDefinition? FormDefinition { get; set; }
}

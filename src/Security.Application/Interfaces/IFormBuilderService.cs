using Security.Domain.Entities;

namespace Security.Application.Interfaces;

public interface IFormBuilderService
{
    Task<FormDefinition?> GetByMenuKeyAsync(string menuKey);
    Task<FormDefinition> UpsertAsync(string menuKey, string name, string fieldsJson);
    Task<List<FormFieldDefinition>> GetFieldsAsync(string menuKey);
    Task<FormSubmission?> GetSubmissionAsync(string menuKey, string recordKey);
    Task SaveSubmissionAsync(string menuKey, string recordKey, Dictionary<string, string?> values);
    Task<ValidationResult> ValidateAsync(string menuKey, Dictionary<string, string?> values);
}

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public Dictionary<string, string> Errors { get; set; } = new();
}

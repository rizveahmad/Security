using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Security.Application.Interfaces;
using Security.Domain.Entities;

namespace Security.Infrastructure.Services;

public class FormBuilderService : IFormBuilderService
{
    private readonly IAppDbContext _db;
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public FormBuilderService(IAppDbContext db) => _db = db;

    public async Task<FormDefinition?> GetByMenuKeyAsync(string menuKey) =>
        await _db.FormDefinitions.FirstOrDefaultAsync(f => f.MenuKey == menuKey);

    public async Task<FormDefinition> UpsertAsync(string menuKey, string name, string fieldsJson)
    {
        var existing = await _db.FormDefinitions.FirstOrDefaultAsync(f => f.MenuKey == menuKey);
        if (existing is null)
        {
            existing = new FormDefinition { MenuKey = menuKey, Name = name, FieldsJson = fieldsJson };
            _db.FormDefinitions.Add(existing);
        }
        else
        {
            existing.Name = name;
            existing.FieldsJson = fieldsJson;
            existing.Version++;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<List<FormFieldDefinition>> GetFieldsAsync(string menuKey)
    {
        var def = await GetByMenuKeyAsync(menuKey);
        if (def is null) return new List<FormFieldDefinition>();
        var fields = JsonSerializer.Deserialize<List<FormFieldDefinition>>(def.FieldsJson, _opts)
                     ?? new List<FormFieldDefinition>();
        return fields.OrderBy(f => f.Order).ToList();
    }

    public async Task<FormSubmission?> GetSubmissionAsync(string menuKey, string recordKey) =>
        await _db.FormSubmissions
            .FirstOrDefaultAsync(s => s.MenuKey == menuKey && s.RecordKey == recordKey);

    public async Task SaveSubmissionAsync(string menuKey, string recordKey, Dictionary<string, string?> values)
    {
        var def = await GetByMenuKeyAsync(menuKey)
                  ?? throw new InvalidOperationException($"No form definition for menu '{menuKey}'");
        var json = JsonSerializer.Serialize(values);
        var sub = await _db.FormSubmissions
            .FirstOrDefaultAsync(s => s.MenuKey == menuKey && s.RecordKey == recordKey);
        if (sub is null)
        {
            sub = new FormSubmission
            {
                MenuKey = menuKey,
                RecordKey = recordKey,
                FormDefinitionId = def.Id,
                ValuesJson = json
            };
            _db.FormSubmissions.Add(sub);
        }
        else
        {
            sub.ValuesJson = json;
            sub.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<ValidationResult> ValidateAsync(string menuKey, Dictionary<string, string?> values)
    {
        var result = new ValidationResult();
        var fields = await GetFieldsAsync(menuKey);
        foreach (var field in fields.Where(f => f.Required))
        {
            if (!values.TryGetValue(field.Key, out var val) || string.IsNullOrWhiteSpace(val))
                result.Errors[field.Key] = $"{field.Label} is required.";
        }
        return result;
    }
}

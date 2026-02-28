namespace Security.Domain.Entities;

public enum FormFieldType
{
    Text,
    Number,
    Dropdown,
    Date,
    Checkbox,
    Textarea
}

public class FormFieldDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public FormFieldType FieldType { get; set; } = FormFieldType.Text;
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public int Order { get; set; }
    public List<DropdownOption> Options { get; set; } = new();
    public string? DynamicSource { get; set; }
}

public class DropdownOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

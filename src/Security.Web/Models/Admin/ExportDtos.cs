namespace Security.Web.Models.Admin;

public class UserExportRow
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IsActive { get; set; } = string.Empty;
}

public class RoleExportRow
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string IsActive { get; set; } = string.Empty;
}

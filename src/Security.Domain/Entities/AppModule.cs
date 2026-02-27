using Security.Domain.Common;

namespace Security.Domain.Entities;
public class AppModule : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public ICollection<AppMenu> Menus { get; set; } = new List<AppMenu>();
}

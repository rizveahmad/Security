namespace Security.Domain.Common;

/// <summary>
/// Base class for all domain entities that require audit tracking and soft-delete support.
/// </summary>
public abstract class AuditableEntity
{
    public int Id { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public string? DeletedBy { get; set; }
    public DateTime? DeletedDate { get; set; }

    public bool IsDeleted => DeletedDate.HasValue;

    public void SoftDelete(string deletedBy)
    {
        DeletedBy = deletedBy;
        DeletedDate = DateTime.UtcNow;
    }
}

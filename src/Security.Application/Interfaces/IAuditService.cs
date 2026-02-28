using Security.Domain.Entities;

namespace Security.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string entityName, AuditAction action, string entityId,
        string? oldValues = null, string? newValues = null);
}

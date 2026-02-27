using Security.Application.Interfaces;
using Security.Domain.Entities;
using Security.Infrastructure.Data;

namespace Security.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string entityName, AuditAction action, string entityId,
        string? oldValues = null, string? newValues = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = entityName,
            Action = action,
            EntityId = entityId,
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            OldValues = oldValues,
            NewValues = newValues,
            Timestamp = DateTime.UtcNow,
            IpAddress = _currentUser.IpAddress
        });
        await _db.SaveChangesAsync();
    }
}

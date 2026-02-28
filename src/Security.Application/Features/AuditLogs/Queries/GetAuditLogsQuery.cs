using MediatR;
using Microsoft.EntityFrameworkCore;
using Security.Application.Common.Interfaces;
using Security.Application.Common.Models;

namespace Security.Application.Features.AuditLogs.Queries;

public record GetAuditLogsQuery(int PageNumber = 1, int PageSize = 20, string? Search = null)
    : IRequest<PaginatedList<AuditLogDto>>;

public record AuditLogDto(int Id, string? UserId, string? UserName, string? Action, string? EntityName, string? EntityId, DateTime Timestamp, string? IPAddress);

public class GetAuditLogsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetAuditLogsQuery, PaginatedList<AuditLogDto>>
{
    public async Task<PaginatedList<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        IQueryable<Security.Domain.Entities.AuditLog> query = context.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(a => (a.UserName != null && a.UserName.Contains(request.Search)) || (a.Action != null && a.Action.Contains(request.Search)) || (a.EntityName != null && a.EntityName.Contains(request.Search)));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.Timestamp)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(a => new AuditLogDto(a.Id, a.UserId, a.UserName, a.Action, a.EntityName, a.EntityId, a.Timestamp, a.IPAddress))
            .ToListAsync(ct);
        return new PaginatedList<AuditLogDto>(items, total, request.PageNumber, request.PageSize);
    }
}

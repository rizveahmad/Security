using Security.Domain.Common;

namespace Security.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the application's write-side persistence context.
/// </summary>
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

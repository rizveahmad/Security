using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;

namespace Security.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<FormDefinition> FormDefinitions { get; }
    DbSet<FormSubmission> FormSubmissions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

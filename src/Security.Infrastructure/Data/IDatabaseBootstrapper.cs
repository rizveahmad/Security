namespace Security.Infrastructure.Data;

/// <summary>
/// Orchestrates the deterministic startup sequence:
/// 1) Ensures the target database exists.
/// 2) Runs all pending numbered SQL scripts.
/// 3) Seeds roles and the Super Admin user.
/// </summary>
public interface IDatabaseBootstrapper
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

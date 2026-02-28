namespace Security.Infrastructure.Data;

/// <summary>
/// Runs numbered SQL scripts against the configured database in lexical order,
/// skipping any scripts already recorded in <c>dbo.ScriptExecutionHistory</c>.
/// </summary>
public interface IDatabaseScriptRunner
{
    Task RunAsync(CancellationToken cancellationToken = default);
}

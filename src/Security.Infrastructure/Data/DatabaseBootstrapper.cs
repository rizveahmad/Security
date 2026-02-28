using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Security.Infrastructure.Data;

/// <summary>
/// Orchestrates deterministic startup:
/// 1) Ensures the target database exists (connects to master and issues CREATE DATABASE if needed).
/// 2) Runs all pending numbered SQL scripts via <see cref="IDatabaseScriptRunner"/>.
/// 3) Seeds roles and the Super Admin via <see cref="DbInitializer"/>.
///
/// Call <see cref="InitializeAsync"/> before <c>app.Run()</c> so the schema and seed data are
/// ready before the first request arrives.
/// </summary>
public sealed class DatabaseBootstrapper : IDatabaseBootstrapper
{
    private readonly IDatabaseScriptRunner _scriptRunner;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _connectionString;
    private readonly ILogger<DatabaseBootstrapper> _logger;

    public DatabaseBootstrapper(
        IDatabaseScriptRunner scriptRunner,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DatabaseBootstrapper> logger)
    {
        _scriptRunner = scriptRunner;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseExistsAsync(cancellationToken);
        await _scriptRunner.RunAsync(cancellationToken);
        await DbInitializer.SeedAsync(_serviceProvider);
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            _logger.LogWarning(
                "DatabaseBootstrapper: no database name found in connection string; skipping auto-create.");
            return;
        }

        builder.InitialCatalog = "master";

        _logger.LogInformation(
            "DatabaseBootstrapper: ensuring database '{Database}' exists.", databaseName);

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Sanitise the database name to prevent SQL injection.
        // Bracket the name (escaping any embedded ']') and escape single quotes for the
        // sys.databases lookup.
        var safeBracketedName = databaseName.Replace("]", "]]");
        var safeSingleQuotedName = databaseName.Replace("'", "''");
        var sql = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{safeSingleQuotedName}') " +
                  $"CREATE DATABASE [{safeBracketedName}];";

        await using var cmd = new SqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("DatabaseBootstrapper: database '{Database}' is ready.", databaseName);
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Security.Infrastructure.Data;

/// <summary>
/// Runs numbered SQL scripts from the configured folder in lexical order, skipping any already
/// recorded in <c>dbo.ScriptExecutionHistory</c>.  Idempotent and production-safe.
///
/// To add a new migration, place a file named <c>NNNN_description.sql</c> in the scripts
/// folder (default: <c>scripts</c> relative to the application content root).
/// </summary>
public sealed class SqlScriptRunner : IDatabaseScriptRunner
{
    private readonly string _connectionString;
    private readonly string _scriptFolder;
    private readonly ILogger<SqlScriptRunner> _logger;

    public SqlScriptRunner(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<SqlScriptRunner> logger)
    {
        _logger = logger;

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");

        // Allow override via config; fall back to <ContentRoot>/scripts.
        var configuredFolder = configuration["ScriptRunner:ScriptFolder"];
        _scriptFolder = string.IsNullOrWhiteSpace(configuredFolder)
            ? Path.Combine(hostEnvironment.ContentRootPath, "scripts")
            : Path.IsPathRooted(configuredFolder)
                ? configuredFolder
                : Path.Combine(hostEnvironment.ContentRootPath, configuredFolder);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SqlScriptRunner starting. Script folder: {Folder}", _scriptFolder);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureHistoryTableAsync(connection, cancellationToken);

        var scripts = GetPendingScripts(_scriptFolder);
        if (scripts.Length == 0)
        {
            _logger.LogInformation("SqlScriptRunner: no pending scripts found.");
            return;
        }

        var executed = await GetExecutedScriptsAsync(connection, cancellationToken);

        foreach (var scriptPath in scripts)
        {
            var fileName = Path.GetFileName(scriptPath);

            if (executed.Contains(fileName))
            {
                _logger.LogDebug("Skipping already-executed script: {Script}", fileName);
                continue;
            }

            await RunScriptAsync(connection, scriptPath, fileName, cancellationToken);
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates <c>dbo.ScriptExecutionHistory</c> if it does not already exist.
    /// This DDL statement is idempotent.
    /// </summary>
    private static async Task EnsureHistoryTableAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (
                SELECT 1
                FROM   sys.tables t
                JOIN   sys.schemas s ON s.schema_id = t.schema_id
                WHERE  s.name = 'dbo'
                AND    t.name = 'ScriptExecutionHistory'
            )
            BEGIN
                CREATE TABLE dbo.ScriptExecutionHistory (
                    Id              INT            IDENTITY(1,1) NOT NULL
                        CONSTRAINT PK_ScriptExecutionHistory PRIMARY KEY,
                    ScriptFileName  NVARCHAR(500)  NOT NULL,
                    ContentHash     NVARCHAR(64)   NOT NULL,
                    ExecutedAt      DATETIME2      NOT NULL
                        CONSTRAINT DF_ScriptExecutionHistory_ExecutedAt DEFAULT SYSUTCDATETIME(),
                    Succeeded       BIT            NOT NULL,
                    ErrorMessage    NVARCHAR(MAX)  NULL
                );

                CREATE UNIQUE INDEX UX_ScriptExecutionHistory_FileName
                    ON dbo.ScriptExecutionHistory (ScriptFileName);
            END
            """;

        await using var cmd = new SqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the set of script file names that have already been successfully executed.
    /// </summary>
    private static async Task<HashSet<string>> GetExecutedScriptsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT ScriptFileName
            FROM   dbo.ScriptExecutionHistory
            WHERE  Succeeded = 1
            """;

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetString(0));

        return result;
    }

    /// <summary>
    /// Scans the script folder for <c>*.sql</c> files and returns them in lexical order.
    /// Returns an empty array when the folder does not exist.
    /// </summary>
    private string[] GetPendingScripts(string folder)
    {
        if (!Directory.Exists(folder))
        {
            _logger.LogWarning(
                "SqlScriptRunner: script folder '{Folder}' not found – skipping.", folder);
            return [];
        }

        return [.. Directory.GetFiles(folder, "*.sql").OrderBy(f => f, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Executes a single SQL script inside a transaction and records the outcome in the
    /// history table.  On failure the transaction is rolled back and the error is recorded;
    /// the exception is then re-thrown so the host fails fast.
    /// </summary>
    private async Task RunScriptAsync(
        SqlConnection connection,
        string scriptPath,
        string fileName,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8, cancellationToken);
        var hash = ComputeSha256(content);

        _logger.LogInformation("Executing script: {Script}", fileName);

        await using var transaction = connection.BeginTransaction();

        try
        {
            // SQL scripts may contain GO batch separators; split and execute each batch.
            var batches = SplitBatches(content);
            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                await using var cmd = new SqlCommand(batch, connection, transaction);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await RecordHistoryAsync(
                connection, transaction, fileName, hash,
                succeeded: true, errorMessage: null,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Script executed successfully: {Script}", fileName);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Script failed: {Script}", fileName);

            // Record the failure outside the rolled-back transaction.
            await RecordFailureOutsideTransactionAsync(
                connection, fileName, hash, ex.Message, cancellationToken);

            throw;
        }
    }

    private static async Task RecordHistoryAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string fileName,
        string hash,
        bool succeeded,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ScriptExecutionHistory
                (ScriptFileName, ContentHash, ExecutedAt, Succeeded, ErrorMessage)
            VALUES
                (@FileName, @Hash, SYSUTCDATETIME(), @Succeeded, @ErrorMessage)
            """;

        await using var cmd = new SqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("@FileName", fileName);
        cmd.Parameters.AddWithValue("@Hash", hash);
        cmd.Parameters.AddWithValue("@Succeeded", succeeded);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RecordFailureOutsideTransactionAsync(
        SqlConnection connection,
        string fileName,
        string hash,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            const string sql = """
                INSERT INTO dbo.ScriptExecutionHistory
                    (ScriptFileName, ContentHash, ExecutedAt, Succeeded, ErrorMessage)
                VALUES
                    (@FileName, @Hash, SYSUTCDATETIME(), 0, @ErrorMessage)
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@FileName", fileName);
            cmd.Parameters.AddWithValue("@Hash", hash);
            cmd.Parameters.AddWithValue("@ErrorMessage", errorMessage);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Best-effort only; don't mask the original exception.
        }
    }

    /// <summary>
    /// Splits a SQL script on GO batch separators (case-insensitive, on its own line).
    /// </summary>
    private static IEnumerable<string> SplitBatches(string sql)
    {
        var lines = sql.Split('\n');
        var batch = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return batch.ToString();
                batch.Clear();
            }
            else
            {
                batch.AppendLine(line);
            }
        }

        if (batch.Length > 0)
            yield return batch.ToString();
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

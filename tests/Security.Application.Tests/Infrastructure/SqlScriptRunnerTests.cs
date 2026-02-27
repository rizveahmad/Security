using Security.Infrastructure.Data;

namespace Security.Application.Tests.Infrastructure;

/// <summary>
/// White-box tests for the SqlScriptRunner helper that do not require a live database.
/// The batch-splitting logic is tested via reflection because it is a private static method.
/// </summary>
public class SqlScriptRunnerTests
{
    [Fact]
    public void SplitBatches_NoGo_ReturnsSingleBatch()
    {
        var sql = "SELECT 1; SELECT 2;";

        var batches = InvokeSplitBatches(sql).ToList();

        Assert.Single(batches);
        Assert.Contains("SELECT 1;", batches[0]);
    }

    [Fact]
    public void SplitBatches_WithGo_ReturnsTwoBatches()
    {
        var sql = "SELECT 1\nGO\nSELECT 2";

        var batches = InvokeSplitBatches(sql).ToList();

        Assert.Equal(2, batches.Count);
        Assert.Contains("SELECT 1", batches[0]);
        Assert.Contains("SELECT 2", batches[1]);
    }

    [Fact]
    public void SplitBatches_GoIsCaseInsensitive()
    {
        var sql = "SELECT 1\ngo\nSELECT 2";

        var batches = InvokeSplitBatches(sql).ToList();

        Assert.Equal(2, batches.Count);
    }

    [Fact]
    public void SplitBatches_MultipleGo_ReturnsMultipleBatches()
    {
        var sql = "A\nGO\nB\nGO\nC";

        var batches = InvokeSplitBatches(sql)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .ToList();

        Assert.Equal(3, batches.Count);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static IEnumerable<string> InvokeSplitBatches(string sql)
    {
        var method = typeof(SqlScriptRunner).GetMethod(
            "SplitBatches",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(null, [sql]);
        Assert.NotNull(result);

        return (IEnumerable<string>)result;
    }
}

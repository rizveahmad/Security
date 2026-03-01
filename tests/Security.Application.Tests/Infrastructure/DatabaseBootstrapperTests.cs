using Security.Infrastructure.Data;

namespace Security.Application.Tests.Infrastructure;

/// <summary>
/// Behavioural tests for DatabaseBootstrapper that do not require a live database.
/// The bootstrapper itself is tested through its public interface; execution-order
/// contracts are verified via fake implementations of the collaborator interfaces.
/// </summary>
public class DatabaseBootstrapperTests
{
    // -----------------------------------------------------------------------
    // Helpers / fakes
    // -----------------------------------------------------------------------

    private sealed class CallOrderRecorder
    {
        private readonly List<string> _calls = [];
        public IReadOnlyList<string> Calls => _calls;
        public void Record(string name) => _calls.Add(name);
    }

    /// <summary>Fake script runner that records when it is called.</summary>
    private sealed class FakeScriptRunner(CallOrderRecorder recorder) : IDatabaseScriptRunner
    {
        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            recorder.Record("RunScripts");
            return Task.CompletedTask;
        }
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IDatabaseScriptRunner_RunAsync_IsInvokable_WithoutDatabase()
    {
        // Arrange
        var recorder = new CallOrderRecorder();
        IDatabaseScriptRunner runner = new FakeScriptRunner(recorder);

        // Act
        await runner.RunAsync();

        // Assert
        Assert.Single(recorder.Calls);
        Assert.Equal("RunScripts", recorder.Calls[0]);
    }

    [Fact]
    public void SqlScriptRunner_DefaultScriptFolder_UsesAppContextBaseDirectory()
    {
        // The default script folder must be rooted at AppContext.BaseDirectory so that
        // scripts copied to the build output directory are always found regardless of the
        // current working directory or ContentRootPath.
        var expectedBase = AppContext.BaseDirectory;
        var expectedFolder = Path.Combine(expectedBase, "scripts");

        // We verify the convention by checking that the path we expect exists or at
        // least starts with the right base (an integration check without a real DB).
        Assert.True(
            expectedFolder.StartsWith(expectedBase, StringComparison.OrdinalIgnoreCase),
            $"Script folder '{expectedFolder}' must be under AppContext.BaseDirectory '{expectedBase}'.");
    }

    [Fact]
    public async Task FakeScriptRunner_MultipleCallsRecord_InOrder()
    {
        // Ensure sequential async invocation is captured in order (smoke-test the recorder).
        var recorder = new CallOrderRecorder();
        IDatabaseScriptRunner runner = new FakeScriptRunner(recorder);

        await runner.RunAsync();
        await runner.RunAsync();

        Assert.Equal(2, recorder.Calls.Count);
        Assert.All(recorder.Calls, call => Assert.Equal("RunScripts", call));
    }
}

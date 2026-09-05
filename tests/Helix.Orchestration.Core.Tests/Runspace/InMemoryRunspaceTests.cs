using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Runspace;

public sealed class InMemoryRunspaceTests
{
    private static RunspaceDefinition EmptyDefinition() => new RunspaceDefinition(
        "test-id", "Test", [], [], new Dictionary<string, object?>());

    [Fact]
    public async Task PauseAsync_SuspendsExecution()
    {
        var counter = 0;
        var started = new TaskCompletionSource();
        InMemoryRunspace? runspace = null;
        runspace = new InMemoryRunspace(EmptyDefinition(), async token =>
        {
            while (!token.IsCancellationRequested)
            {
                await runspace!.CheckPauseAsync(token);
                counter++;
                started.TrySetResult();
                await Task.Delay(10, token);
            }
        });

        await runspace.StartAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await runspace.PauseAsync();
        await Task.Delay(50);

        var snapshot = counter;
        await Task.Delay(200);

        // Counter must not have advanced while paused
        Assert.Equal(snapshot, counter);

        await runspace.ResumeAsync();
        await Task.Delay(50);

        // Counter advances again after resume
        Assert.True(counter > snapshot);

        await runspace.AbortAsync();
    }

    [Fact]
    public async Task ResumeAsync_ContinuesExecution_RunspaceReachesCompleted()
    {
        // Body blocks on a gate that we control, then calls CheckPauseAsync, then completes.
        var blockUntilTest = new SemaphoreSlim(0, 1);
        InMemoryRunspace? runspace = null;
        runspace = new InMemoryRunspace(EmptyDefinition(), async token =>
        {
            await blockUntilTest.WaitAsync(token); // wait until test releases
            await runspace!.CheckPauseAsync(token);
        });

        await runspace.StartAsync();

        // Now pause, then release the block, then resume.
        await runspace.PauseAsync();
        Assert.Equal(RunspaceStatus.Paused, runspace.Status);

        blockUntilTest.Release(); // let body proceed to CheckPauseAsync
        await runspace.ResumeAsync(); // release the pause gate

        await runspace.WaitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);

        Assert.Equal(RunspaceStatus.Completed, runspace.Status);
    }

    [Fact]
    public async Task AbortAsync_WhilePaused_CompletesCleanly()
    {
        InMemoryRunspace? runspace = null;
        runspace = new InMemoryRunspace(EmptyDefinition(), async token =>
        {
            await runspace!.CheckPauseAsync(token);
            await Task.Delay(Timeout.Infinite, token);
        });

        await runspace.StartAsync();
        await Task.Delay(10);

        await runspace.PauseAsync();
        await runspace.AbortAsync();

        Assert.Equal(RunspaceStatus.Faulted, runspace.Status);
        Assert.IsAssignableFrom<OperationCanceledException>(runspace.Fault);
    }
}

using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Runspace;

public sealed class RunspaceManagerTests
{
    [Fact]
    public async Task CreateAsync_TracksRunspace()
    {
        await using var manager = new RunspaceManager();
        var definition = CreateDefinition("wf-create");

        var runspace = await manager.CreateAsync(definition);

        Assert.Same(runspace, manager.Get(runspace.RunspaceId));
        Assert.Contains(runspace, manager.ListActive());
    }

    [Fact]
    public async Task AbortAsync_RemovesRunspace()
    {
        await using var manager = new RunspaceManager();
        var definition = CreateDefinition("wf-abort");
        var runspace = await manager.CreateAsync(definition);

        await manager.AbortAsync(runspace.RunspaceId).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Null(manager.Get(runspace.RunspaceId));
        Assert.Empty(manager.ListActive());
    }

    [Fact]
    public async Task CreateAsync_DuplicateRunspaceId_ThrowsInvalidOperationException()
    {
        var id = "fixed-runspace-id";
        await using var manager = new RunspaceManager((definition, cancellationToken) =>
            Task.FromResult<IRunspace>(new FixedIdRunspace(id, definition)));

        var first = await manager.CreateAsync(CreateDefinition("wf-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.CreateAsync(CreateDefinition("wf-2")));

        Assert.Same(first, manager.Get(id));
    }

    private static RunspaceDefinition CreateDefinition(string name) => new(
        DefinitionId: name,
        Name: name,
        Nodes: [],
        Edges: [],
        Metadata: new Dictionary<string, object?>());

    private sealed class FixedIdRunspace : IRunspace
    {
        public FixedIdRunspace(string runspaceId, RunspaceDefinition definition)
        {
            RunspaceId = runspaceId;
            Definition = definition;
        }

        public string RunspaceId { get; }
        public RunspaceDefinition Definition { get; }
        public RunspaceStatus Status { get; private set; } = RunspaceStatus.Idle;
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? StartedAt { get; private set; }
        public DateTimeOffset? EndedAt { get; private set; }
        public Exception? Fault { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Status = RunspaceStatus.Running;
            StartedAt = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }

        public Task PauseAsync(CancellationToken cancellationToken = default)
        {
            Status = RunspaceStatus.Paused;
            return Task.CompletedTask;
        }

        public Task ResumeAsync(CancellationToken cancellationToken = default)
        {
            Status = RunspaceStatus.Running;
            return Task.CompletedTask;
        }

        public Task AbortAsync(CancellationToken cancellationToken = default)
        {
            Status = RunspaceStatus.Faulted;
            EndedAt = DateTimeOffset.UtcNow;
            Fault = new OperationCanceledException();
            return Task.CompletedTask;
        }

        public Task WaitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync()
        {
            Status = RunspaceStatus.Disposed;
            return ValueTask.CompletedTask;
        }
    }
}

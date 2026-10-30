using Grpc.Core;
using Helix.Orchestration.Agent.GrpcServer;
using Helix.Orchestration.Agent.Runspaces;
using Helix.Orchestration.Contracts.Control;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Agent.Experiments;
using Moq;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

/// <summary>Production transport compatibility must never turn a raw graph into authority.</summary>
public sealed class DispatchBoundaryTests
{
    [Fact]
    public async Task LegacyDurableRunspace_RejectsBeforeInvokingReplayableExecutor()
    {
        var executor = new Mock<IWorkflowExecutor>(MockBehavior.Strict);
        executor.Setup(value => value.CancelAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var definition = new Helix.Orchestration.Core.WSL.RunspaceDefinition(
            "definition-1", "checked", [], [], new Dictionary<string, object?>());
        await using var runspace = new DurableRunspace(definition, executor.Object);

        await Assert.ThrowsAsync<NotSupportedException>(() => runspace.StartAsync());
        executor.Verify(value => value.ExecuteAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Never);

        await runspace.AbortAsync();
        Assert.Equal(RunspaceStatus.Faulted, runspace.Status);
    }

    [Fact]
    public async Task SubmitExperiment_RawDefinition_IsRejectedBeforeRunspaceCreation()
    {
        var manager = new Mock<IRunspaceManager>(MockBehavior.Strict);
        var service = new AgentGrpcService(manager.Object, new DeviceRegistry());

        var result = await service.SubmitExperiment(
            new ExperimentSubmission { RunspaceDefinitionJson = "{\"name\":\"forged\"}" },
            new TestServerCallContext());

        Assert.False(result.Accepted);
        Assert.Contains("Raw workflow definitions", result.Message, StringComparison.Ordinal);
        manager.Verify(manager => manager.CreateAsync(
            It.IsAny<Helix.Orchestration.Core.WSL.RunspaceDefinition>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitExperiment_CanonicalExperimentReference_CreatesOnlyTheAgentDefinition()
    {
        var definition = new Helix.Orchestration.Core.WSL.RunspaceDefinition(
            "definition-1", "checked", [], [], new Dictionary<string, object?>());
        var experiments = new Mock<IExperimentManager>(MockBehavior.Strict);
        experiments.Setup(manager => manager.TryGetCanonicalDefinition("experiment-1", out It.Ref<CanonicalExperimentDefinition?>.IsAny))
            .Returns((string _, out CanonicalExperimentDefinition? canonical) =>
            {
                canonical = new CanonicalExperimentDefinition(definition, "policy-1", "hash", "actor", DateTimeOffset.UtcNow);
                return true;
            });
        experiments.Setup(manager => manager.SetActiveRunspace("experiment-1", "runspace-1"));

        var runspace = new Mock<IRunspace>(MockBehavior.Strict);
        runspace.SetupGet(value => value.RunspaceId).Returns("runspace-1");
        var manager = new Mock<IRunspaceManager>(MockBehavior.Strict);
        manager.Setup(value => value.CreateAsync(definition, It.IsAny<CancellationToken>())).ReturnsAsync(runspace.Object);
        var service = new AgentGrpcService(manager.Object, new DeviceRegistry(), experiments.Object);

        var result = await service.SubmitExperiment(
            new ExperimentSubmission { RunspaceDefinitionJson = "experiment-1" },
            new TestServerCallContext());

        Assert.True(result.Accepted);
        Assert.Equal("runspace-1", result.RunspaceId);
        manager.Verify(value => value.CreateAsync(definition, It.IsAny<CancellationToken>()), Times.Once);
    }
}

file sealed class TestServerCallContext : ServerCallContext
{
    protected override string MethodCore => "/test";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "127.0.0.1";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => [];
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override Metadata ResponseTrailersCore => [];
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore => new(null, []);
    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}

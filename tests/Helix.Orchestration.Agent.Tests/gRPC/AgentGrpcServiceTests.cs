using Grpc.Core;
using Helix.Orchestration.Agent.GrpcServer;
using Helix.Orchestration.Contracts.Control;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Runspace;
using Moq;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.gRPC;

public sealed class AgentGrpcServiceTests
{
    private static ServerCallContext FakeContext() => new FakeServerCallContext();

    [Fact]
    public async Task PauseRunspace_ValidId_CallsPauseAsyncAndReturnsSuccess()
    {
        var mockRunspace = new Mock<IRunspace>();
        mockRunspace.Setup(r => r.PauseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var manager = new Mock<IRunspaceManager>();
        manager.Setup(m => m.Get("rs-1")).Returns(mockRunspace.Object);

        var svc = new AgentGrpcService(manager.Object, new DeviceRegistry());
        var ack = await svc.PauseRunspace(new RunspaceCommand { RunspaceId = "rs-1" }, FakeContext());

        Assert.True(ack.Success);
        Assert.Equal("Paused", ack.Message);
        mockRunspace.Verify(r => r.PauseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PauseRunspace_UnknownId_ReturnsFailureWithNotFound()
    {
        var manager = new Mock<IRunspaceManager>();
        manager.Setup(m => m.Get("unknown")).Returns((IRunspace?)null);

        var svc = new AgentGrpcService(manager.Object, new DeviceRegistry());
        var ack = await svc.PauseRunspace(new RunspaceCommand { RunspaceId = "unknown" }, FakeContext());

        Assert.False(ack.Success);
        Assert.Contains("not found", ack.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PauseRunspace_PauseAsyncThrows_ReturnsFailure()
    {
        var mockRunspace = new Mock<IRunspace>();
        mockRunspace.Setup(r => r.PauseAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("already paused"));

        var manager = new Mock<IRunspaceManager>();
        manager.Setup(m => m.Get("rs-2")).Returns(mockRunspace.Object);

        var svc = new AgentGrpcService(manager.Object, new DeviceRegistry());
        var ack = await svc.PauseRunspace(new RunspaceCommand { RunspaceId = "rs-2" }, FakeContext());

        Assert.False(ack.Success);
        Assert.Contains("already paused", ack.Message);
    }

    [Fact]
    public async Task ResumeRunspace_ValidId_CallsResumeAsyncAndReturnsSuccess()
    {
        var mockRunspace = new Mock<IRunspace>();
        mockRunspace.Setup(r => r.ResumeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var manager = new Mock<IRunspaceManager>();
        manager.Setup(m => m.Get("rs-3")).Returns(mockRunspace.Object);

        var svc = new AgentGrpcService(manager.Object, new DeviceRegistry());
        var ack = await svc.ResumeRunspace(new RunspaceCommand { RunspaceId = "rs-3" }, FakeContext());

        Assert.True(ack.Success);
        Assert.Equal("Resumed", ack.Message);
        mockRunspace.Verify(r => r.ResumeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResumeRunspace_UnknownId_ReturnsFailureWithNotFound()
    {
        var manager = new Mock<IRunspaceManager>();
        manager.Setup(m => m.Get("unknown")).Returns((IRunspace?)null);

        var svc = new AgentGrpcService(manager.Object, new DeviceRegistry());
        var ack = await svc.ResumeRunspace(new RunspaceCommand { RunspaceId = "unknown" }, FakeContext());

        Assert.False(ack.Success);
        Assert.Contains("not found", ack.Message, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Minimal ServerCallContext stub for unit testing gRPC service methods.
/// Only provides a non-cancellable CancellationToken; all other members are unused.
/// </summary>
file sealed class FakeServerCallContext : ServerCallContext
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
    protected override AuthContext AuthContextCore => new AuthContext(null, []);
    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
        => throw new NotSupportedException();
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}

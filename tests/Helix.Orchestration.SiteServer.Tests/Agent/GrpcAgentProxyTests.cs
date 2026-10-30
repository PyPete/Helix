using Helix.Orchestration.Contracts.Control;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.SiteServer.Agent;
using Moq;
using Xunit;

namespace Helix.Orchestration.SiteServer.Tests.Agent;

public class GrpcAgentProxyTests
{
    private static RunspaceDefinition MinimalDefinition(string id = "test-exp") =>
        new(
            DefinitionId: id,
            Name: "Test Experiment",
            Nodes: Array.Empty<WslNode>(),
            Edges: Array.Empty<WslEdge>(),
            Metadata: new Dictionary<string, object?>());

    [Fact]
    public async Task SubmitExperimentAsync_AcceptedResponse_ReturnsRunspaceId()
    {
        var client = new Mock<IAgentControlClient>();
        client
            .Setup(c => c.SubmitExperimentAsync(
                It.Is<ExperimentSubmission>(r => !string.IsNullOrWhiteSpace(r.RunspaceDefinitionJson)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionAck
            {
                Accepted = true,
                RunspaceId = "runspace-123",
                Message = "Accepted"
            });

        var proxy = new GrpcAgentProxy(client.Object);

        var runspaceId = await proxy.SubmitExperimentAsync(MinimalDefinition());

        Assert.Equal("runspace-123", runspaceId);
    }

    [Fact]
    public async Task SubmitExperimentAsync_RejectedResponse_ThrowsInvalidOperationException()
    {
        var client = new Mock<IAgentControlClient>();
        client
            .Setup(c => c.SubmitExperimentAsync(
                It.IsAny<ExperimentSubmission>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionAck
            {
                Accepted = false,
                Message = "Rejected"
            });

        var proxy = new GrpcAgentProxy(client.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.SubmitExperimentAsync(MinimalDefinition()));

        Assert.Equal("Rejected", ex.Message);
    }

    [Fact]
    public async Task GetRunspaceStatusAsync_NotFoundStatus_ThrowsKeyNotFoundException()
    {
        var client = new Mock<IAgentControlClient>();
        client
            .Setup(c => c.GetRunspaceStatusAsync(
                It.IsAny<RunspaceQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunspaceStatusResponse
            {
                RunspaceId = "missing",
                Status = "NotFound",
                Message = "Runspace not found."
            });

        var proxy = new GrpcAgentProxy(client.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => proxy.GetRunspaceStatusAsync("missing"));
    }

    [Fact]
    public async Task AbortRunspaceAsync_FailedAck_ThrowsInvalidOperationException()
    {
        var client = new Mock<IAgentControlClient>();
        client
            .Setup(c => c.AbortRunspaceAsync(
                It.IsAny<RunspaceCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandAck
            {
                Success = false,
                Message = "Cannot abort"
            });

        var proxy = new GrpcAgentProxy(client.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.AbortRunspaceAsync("runspace-1"));

        Assert.Equal("Cannot abort", ex.Message);
    }

    [Fact]
    public async Task GetRunspaceStatusAsync_ValidResponse_MapsToDto()
    {
        var client = new Mock<IAgentControlClient>();
        client
            .Setup(c => c.GetRunspaceStatusAsync(
                It.IsAny<RunspaceQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunspaceStatusResponse
            {
                RunspaceId = "runspace-42",
                Status = "Running",
                Message = "OK"
            });

        var proxy = new GrpcAgentProxy(client.Object);

        var status = await proxy.GetRunspaceStatusAsync("runspace-42");

        Assert.Equal("runspace-42", status.RunspaceId);
        Assert.Equal("Running", status.Status);
        Assert.Null(status.FaultMessage);
    }

    [Fact]
    public async Task GetRegisteredDevicesAsync_ReturnsDevicesFromAgent()
    {
        var client = new Mock<IAgentControlClient>();
        var response = new ListDevicesResponse();
        var device = new DeviceInfo
        {
            DeviceId = "pump-001",
            DeviceName = "Lab Pump",
            DeviceKind = "Pump"
        };
        device.Capabilities.Add("SetFlowRate");
        device.Capabilities.Add("GetFlowRate");
        response.Devices.Add(device);

        client
            .Setup(c => c.ListDevicesAsync(It.IsAny<ListDevicesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var proxy = new GrpcAgentProxy(client.Object);

        var devices = (await proxy.GetRegisteredDevicesAsync()).ToList();

        Assert.Single(devices);
        Assert.Equal("pump-001", devices[0].DeviceId);
        Assert.Equal("Lab Pump", devices[0].DeviceName);
        Assert.Equal("Pump", devices[0].DeviceKind);
        Assert.Contains("SetFlowRate", devices[0].Capabilities);
    }
}

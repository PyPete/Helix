using Microsoft.Extensions.Logging.Abstractions;
using Helix.Orchestration.Core.Trends;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Simulation;
using Helix.Orchestration.Simulation.Devices;
using Helix.Orchestration.SiteServer.Agent;
using Xunit;

namespace Helix.Orchestration.SiteServer.Tests.Agent;

public class SimAgentProxyTests
{
    private static SimAgentProxy CreateProxy()
    {
        var provider = new SimulatedTrendProvider();
        return new SimAgentProxy(provider);
    }

    private static RunspaceDefinition MinimalDefinition(string id = "test-exp") =>
        new RunspaceDefinition(
            DefinitionId: id,
            Name: "Test Experiment",
            Nodes: Array.Empty<WslNode>(),
            Edges: Array.Empty<WslEdge>(),
            Metadata: new Dictionary<string, object?>());

    [Fact]
    public async Task SubmitExperimentAsync_ReturnsNonEmptyRunspaceId()
    {
        var proxy = CreateProxy();

        var id = await proxy.SubmitExperimentAsync(MinimalDefinition());

        Assert.False(string.IsNullOrWhiteSpace(id));
    }

    [Fact]
    public async Task SubmitExperimentAsync_TwoCalls_ReturnDifferentIds()
    {
        var proxy = CreateProxy();

        var id1 = await proxy.SubmitExperimentAsync(MinimalDefinition("exp-1"));
        var id2 = await proxy.SubmitExperimentAsync(MinimalDefinition("exp-2"));

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public async Task GetRunspaceStatusAsync_AfterSubmit_ReturnsKnownRunspace()
    {
        var proxy = CreateProxy();
        var runspaceId = await proxy.SubmitExperimentAsync(MinimalDefinition());

        var status = await proxy.GetRunspaceStatusAsync(runspaceId);

        Assert.Equal(runspaceId, status.RunspaceId);
        Assert.NotNull(status.Status);
    }

    [Fact]
    public async Task GetRunspaceStatusAsync_UnknownId_ThrowsKeyNotFound()
    {
        var proxy = CreateProxy();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => proxy.GetRunspaceStatusAsync("nonexistent-id"));
    }

    [Fact]
    public async Task AbortRunspaceAsync_AfterSubmit_DoesNotThrow()
    {
        var proxy = CreateProxy();
        var runspaceId = await proxy.SubmitExperimentAsync(MinimalDefinition());

        var ex = await Record.ExceptionAsync(() => proxy.AbortRunspaceAsync(runspaceId));

        Assert.Null(ex);
    }

    [Fact]
    public async Task AbortRunspaceAsync_UnknownId_ThrowsKeyNotFound()
    {
        var proxy = CreateProxy();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => proxy.AbortRunspaceAsync("nonexistent-id"));
    }

    [Fact]
    public async Task GetRegisteredDevicesAsync_ReturnsThreeSimDevices()
    {
        var proxy = CreateProxy();

        var devices = (await proxy.GetRegisteredDevicesAsync()).ToList();

        Assert.Equal(3, devices.Count);
        Assert.Contains(devices, d => d.DeviceKind == "Reactor");
        Assert.Contains(devices, d => d.DeviceKind == "Pump");
        Assert.Contains(devices, d => d.DeviceKind == "Spectrometer");
    }

    [Fact]
    public async Task GetRegisteredDevicesAsync_ReactorHasExpectedCapabilities()
    {
        var proxy = CreateProxy();

        var devices = await proxy.GetRegisteredDevicesAsync();
        var reactor = devices.Single(d => d.DeviceKind == "Reactor");

        Assert.Contains("SetTemperature", reactor.Capabilities);
        Assert.Contains("GetTemperature", reactor.Capabilities);
        Assert.Contains("GetPressure", reactor.Capabilities);
    }

    [Fact]
    public async Task GetRunspaceStatusAsync_AfterSubmit_StatusIsRunningOrComplete()
    {
        var proxy = CreateProxy();
        var runspaceId = await proxy.SubmitExperimentAsync(MinimalDefinition());

        // Give the background task a brief opportunity to start
        await Task.Delay(50);

        var status = await proxy.GetRunspaceStatusAsync(runspaceId);

        Assert.True(
            status.Status is "Running" or "Completed" or "Idle",
            $"Unexpected status: {status.Status}");
    }
}

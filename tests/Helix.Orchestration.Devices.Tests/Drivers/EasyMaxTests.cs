using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class EasyMaxTests
{
    // ── Test helpers ──────────────────────────────────────────────────────────

    private static (EasyMax driver, IMettlerOpcCommand opc) CreateDriver()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var opc = Substitute.For<IMettlerOpcCommand>();
        var driver = new EasyMax(transport, opc, NullLogger<EasyMax>.Instance);
        return (driver, opc);
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    // ── FindActiveSystems ─────────────────────────────────────────────────────

    [Fact]
    public async Task FindActiveSystems_ReturnsOnlyRealExperiments()
    {
        var (driver, opc) = CreateDriver();

        // Matches POP EasyMaxTests.SetupMettlerOpc: 4 experiments, 2 real, 2 BadOutOfService
        opc.ConnectedDevices("ns=2;s=Local.iControl", "Experiment")
           .Returns(new List<string> { "Experiment1", "Experiment2", "Experiment3", "Experiment4" });
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment1.ExperimentName").Returns("Experiment 2023-03-13 15-32");
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment2.ExperimentName").Returns("Experiment 2023-03-13 15-32");
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment3.ExperimentName").Returns("BadOutOfService");
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment4.ExperimentName").Returns("BadOutOfService");
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment1.DocumentName").Returns("Experiment 2023-03-13 15-32");
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment2.DocumentName").Returns("Experiment 2023-03-13 15-32");
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment3.DocumentName").Returns("BadOutOfService");
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment4.DocumentName").Returns("BadOutOfService");

        var result = await driver.InvokeCapabilityAsync("FindActiveSystems", Json("{}"));

        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("Experiment1", json);
        Assert.Contains("Experiment2", json);
        Assert.DoesNotContain("Experiment3", json);
        Assert.DoesNotContain("Experiment4", json);
    }

    // ── CreateSubscription ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSubscription_ValidSystem_ReturnsSuccess()
    {
        var (driver, opc) = CreateDriver();

        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment1.ExperimentName").Returns("Experiment 2023-03-13 15-32");
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment1.DocumentName").Returns("Experiment 2023-03-13 15-32");

        var result = await driver.InvokeCapabilityAsync("CreateSubscription", Json("{\"systemName\":\"Experiment1\"}"));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateSubscription_BadOutOfService_ReturnsFailure()
    {
        var (driver, opc) = CreateDriver();

        opc.GetAtNode<string>("ns=2;Local.iControl.BadExp.ExperimentName").Returns("BadOutOfService");
        opc.GetAtNode<string>("ns=2;Local.iControl.BadExp.DocumentName").Returns("BadOutOfService");

        var result = await driver.InvokeCapabilityAsync("CreateSubscription", Json("{\"systemName\":\"BadExp\"}"));

        Assert.False(result.Success);
        Assert.Contains("not live", result.ErrorMessage ?? "");
    }

    // ── IsDevice ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsDevice_Connected_ReturnsSuccess()
    {
        var (driver, opc) = CreateDriver();

        // Node ID from POP EasyMax: ns=2;Local.iControl.ConnectionStatus
        opc.GetAtNode<bool>("ns=2;Local.iControl.ConnectionStatus").Returns(true);

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.True(result.Success);
        opc.Received(1).GetAtNode<bool>("ns=2;Local.iControl.ConnectionStatus");
    }

    [Fact]
    public async Task IsDevice_NotConnected_ReturnsFailure()
    {
        var (driver, opc) = CreateDriver();

        opc.GetAtNode<bool>("ns=2;Local.iControl.ConnectionStatus").Returns(false);

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.False(result.Success);
    }

    // ── GetTrendValues ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTrendValues_WithActiveSystem_ReturnsTrends()
    {
        var (driver, opc) = CreateDriver();

        // First subscribe to set _activeSystem
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment1.ExperimentName").Returns("Experiment 2023-03-13 15-32");
        opc.GetAtNode<string>("ns=2;Local.iControl.Experiment1.DocumentName").Returns("Experiment 2023-03-13 15-32");
        await driver.InvokeCapabilityAsync("CreateSubscription", Json("{\"systemName\":\"Experiment1\"}"));

        // Now mock trend response — node ID from POP EasyMax.GetTrends
        opc.GetTrendsAtNode("ns=2;s=Local.iControl.Experiment1.Trends")
           .Returns(new List<(string, double, string)> { ("Tr", 20.79, "oC"), ("Tr-Tj", -0.917, "K") });

        var result = await driver.InvokeCapabilityAsync("GetTrendValues", Json("{}"));

        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("Tr", json);
        Assert.Contains("20.79", json);
        opc.Received().GetTrendsAtNode("ns=2;s=Local.iControl.Experiment1.Trends");
    }

    [Fact]
    public async Task GetTrendValues_NoActiveSystem_ReturnsFailure()
    {
        var (driver, _) = CreateDriver();

        var result = await driver.InvokeCapabilityAsync("GetTrendValues", Json("{}"));

        Assert.False(result.Success);
        Assert.Contains("CreateSubscription", result.ErrorMessage ?? "");
    }
}

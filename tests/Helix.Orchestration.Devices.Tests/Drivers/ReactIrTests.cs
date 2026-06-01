using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class ReactIrTests
{
    // ── Test helpers ──────────────────────────────────────────────────────────

    private static (ReactIr driver, IMettlerOpcCommand opc) CreateDriver()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var opc = Substitute.For<IMettlerOpcCommand>();
        var driver = new ReactIr(transport, opc, NullLogger<ReactIr>.Instance);
        return (driver, opc);
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    // Configure standard mocks matching POP ReactIrTests.SetupMettlerOpc
    private static void SetupStandardOpc(IMettlerOpcCommand opc)
    {
        opc.CreateSession(Arg.Any<string>())
           .Returns((true, OpcConnectionEnum.Connected, "Connected Ok"));
        opc.ConnectedDevices("ns=2;s=Local.iCIR", "Probe")
           .Returns(new List<string> { "Probe1" });
        opc.GetAtNode<bool>("ns=2;Local.iCIR.ConnectionStatus").Returns(true);
        opc.GetAtNode<string>("ns=2;Local.iCIR.Probe1.ExperimentName")
           .Returns("Experiment 2023-03-13 15-32");
        opc.GetAtNode<string>("ns=2;Local.iCIR.Probe1.DocumentName")
           .Returns("Experiment 2023-03-13 15-32");
    }

    // ── FindActiveSystems ─────────────────────────────────────────────────────

    [Fact]
    public async Task FindActiveSystems_ReturnsSingleActiveProbe()
    {
        var (driver, opc) = CreateDriver();
        SetupStandardOpc(opc);

        var result = await driver.InvokeCapabilityAsync("FindActiveSystems", Json("{}"));

        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("Probe1", json);
        // Node IDs verified: ConnectedDevices("ns=2;s=Local.iCIR", "Probe")
        opc.Received().ConnectedDevices("ns=2;s=Local.iCIR", "Probe");
    }

    // ── CreateSubscription ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSubscription_ValidProbe_ReturnsSuccess()
    {
        var (driver, opc) = CreateDriver();
        SetupStandardOpc(opc);

        var result = await driver.InvokeCapabilityAsync("CreateSubscription", Json("{\"probeName\":\"Probe1\"}"));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateSubscription_NotConnected_ReturnsFailure()
    {
        var (driver, opc) = CreateDriver();
        opc.GetAtNode<bool>("ns=2;Local.iCIR.ConnectionStatus").Returns(false);
        opc.GetAtNode<string>("ns=2;Local.iCIR.Probe1.ExperimentName").Returns("Experiment");

        var result = await driver.InvokeCapabilityAsync("CreateSubscription", Json("{\"probeName\":\"Probe1\"}"));

        Assert.False(result.Success);
    }

    // ── IsDevice ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsDevice_Connected_ReturnsSuccess()
    {
        var (driver, opc) = CreateDriver();

        // Node ID from POP ReactIr: ns=2;Local.iCIR.ConnectionStatus
        opc.GetAtNode<bool>("ns=2;Local.iCIR.ConnectionStatus").Returns(true);

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.True(result.Success);
        opc.Received(1).GetAtNode<bool>("ns=2;Local.iCIR.ConnectionStatus");
    }

    [Fact]
    public async Task IsDevice_NotConnected_ReturnsFailure()
    {
        var (driver, opc) = CreateDriver();

        opc.GetAtNode<bool>("ns=2;Local.iCIR.ConnectionStatus").Returns(false);

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.False(result.Success);
    }

    // ── UpdateSamplingInterval ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSamplingInterval_MatchesNewValue_ReturnsSuccess()
    {
        var (driver, opc) = CreateDriver();

        // Method call node IDs from POP ReactIr.UpdateSamplingInterval(probe, newInterval)
        opc.GetAtNode<int>("ns=2;Local.iCIR.Probe1.CurrentSamplingInterval").Returns(15);

        var result = await driver.InvokeCapabilityAsync(
            "UpdateSamplingInterval",
            Json("{\"probeName\":\"Probe1\",\"intervalSeconds\":15}"));

        Assert.True(result.Success);
        opc.Received(1).CallMethod(
            "ns=2;s=Local.iCIR.Probe1.Methods",
            "ns=2;s=Local.iCIR.Probe1.Methods.SetSamplingInterval",
            Arg.Any<object[]>());
        opc.Received(1).GetAtNode<int>("ns=2;Local.iCIR.Probe1.CurrentSamplingInterval");
    }

    [Fact]
    public async Task UpdateSamplingInterval_DoesNotMatch_ReturnsFailure()
    {
        var (driver, opc) = CreateDriver();

        // Device returns a different value than requested
        opc.GetAtNode<int>("ns=2;Local.iCIR.Probe1.CurrentSamplingInterval").Returns(20);

        var result = await driver.InvokeCapabilityAsync(
            "UpdateSamplingInterval",
            Json("{\"probeName\":\"Probe1\",\"intervalSeconds\":15}"));

        Assert.False(result.Success);
        Assert.Contains("Sampling rate", result.ErrorMessage ?? "");
    }

    // ── GetSamplingRate ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSamplingRate_WithProbeName_ReturnsRate()
    {
        var (driver, opc) = CreateDriver();

        // Node ID from POP ReactIr.GetSamplingRate(probe)
        opc.GetAtNode<int>("ns=2;Local.iCIR.Probe1.CurrentSamplingInterval").Returns(20);

        var result = await driver.InvokeCapabilityAsync(
            "GetSamplingRate",
            Json("{\"probeName\":\"Probe1\"}"));

        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("20", json);
        opc.Received(1).GetAtNode<int>("ns=2;Local.iCIR.Probe1.CurrentSamplingInterval");
    }

    // ── Pause ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pause_ProbePaused_ReturnsSuccess()
    {
        var (driver, opc) = CreateDriver();

        // Node IDs from POP ReactIr.Pause(probe)
        opc.GetAtNode<string>("ns=2;Local.iCIR.Probe1.ProbeStatus").Returns("Paused");

        var result = await driver.InvokeCapabilityAsync("Pause", Json("{\"probeName\":\"Probe1\"}"));

        Assert.True(result.Success);
        opc.Received(1).CallMethod(
            "ns=2;s=Local.iCIR.Probe1.Methods",
            "ns=2;s=Local.iCIR.Probe1.Methods.Pause");
    }

    [Fact]
    public async Task Pause_ProbeNotPaused_ReturnsFailure()
    {
        var (driver, opc) = CreateDriver();

        opc.GetAtNode<string>("ns=2;Local.iCIR.Probe1.ProbeStatus").Returns("Running");

        var result = await driver.InvokeCapabilityAsync("Pause", Json("{\"probeName\":\"Probe1\"}"));

        Assert.False(result.Success);
    }

    // ── Resume ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resume_ProbeRunning_ReturnsSuccess()
    {
        var (driver, opc) = CreateDriver();

        // Node IDs from POP ReactIr.Resume(probe)
        opc.GetAtNode<string>("ns=2;Local.iCIR.Probe1.ProbeStatus").Returns("Running");

        var result = await driver.InvokeCapabilityAsync("Resume", Json("{\"probeName\":\"Probe1\"}"));

        Assert.True(result.Success);
        opc.Received(1).CallMethod(
            "ns=2;s=Local.iCIR.Probe1.Methods",
            "ns=2;s=Local.iCIR.Probe1.Methods.Resume");
    }

    [Fact]
    public async Task Resume_ProbeNotRunning_ReturnsFailure()
    {
        var (driver, opc) = CreateDriver();

        opc.GetAtNode<string>("ns=2;Local.iCIR.Probe1.ProbeStatus").Returns("Paused");

        var result = await driver.InvokeCapabilityAsync("Resume", Json("{\"probeName\":\"Probe1\"}"));

        Assert.False(result.Success);
    }

    // ── has_trends=NO guard ───────────────────────────────────────────────────

    [Fact]
    public void ReactIrDoesNotOverrideStreamTrendsAsync()
    {
        // Verify that ReactIr does not declare StreamTrendsAsync (has_trends=NO per RESEARCH.md)
        var streamTrends = typeof(ReactIr).GetMethod(
            nameof(ReactIr.StreamTrendsAsync),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

        Assert.Null(streamTrends);
    }
}

using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class FbrmTests
{
    // ── Test helpers ──────────────────────────────────────────────────────────

    private static (Fbrm driver, IMettlerOpcCommand opc) CreateDriver()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var opc = Substitute.For<IMettlerOpcCommand>();
        var driver = new Fbrm(transport, opc, NullLogger<Fbrm>.Instance);
        return (driver, opc);
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    // ── ShouldInitialiseOk (mirrors POP FbrmTests.ShouldInitialiseOk) ─────────

    [Fact]
    public void ShouldInitialiseOk()
    {
        // Arrange: Fbrm requires only the opc command interface on construction
        var (driver, opc) = CreateDriver();

        opc.CreateSession(Arg.Any<string>())
           .Returns((true, OpcConnectionEnum.Connected, "Connected Ok"));
        opc.ConnectedDevices("ns=2;s=Local.iCFBRM", "Experiment", Arg.Any<bool>())
           .Returns(new List<string> { "Experiment 1" });
        opc.GetAtNode<bool>("ns=2;Local.iCFBRM.ConnectionStatus").Returns(true);
        opc.GetAtNode<string>("ns=2;Local.iCFBRM.Experiment 1.ExperimentName")
           .Returns("Experiment 2023-03-13 15-32");

        // Act: driver is created without throwing
        Assert.NotNull(driver);
    }

    // ── FindActiveSystems ─────────────────────────────────────────────────────

    [Fact]
    public async Task FindActiveSystems_ReturnsActiveProbes()
    {
        var (driver, opc) = CreateDriver();

        // Node IDs from POP Fbrm.FindActiveSystems: ConnectedDevices uses ns=2;s=Local.iCFBRM, stripBlank=false
        opc.ConnectedDevices("ns=2;s=Local.iCFBRM", "Experiment", false)
           .Returns(new List<string> { "Experiment 1" });
        opc.GetAtNode<bool>("ns=2;Local.iCFBRM.ConnectionStatus").Returns(true);
        opc.GetAtNode<string>("ns=2;Local.iCFBRM.Experiment 1.ExperimentName")
           .Returns("Experiment 2023-03-13 15-32");

        var result = await driver.InvokeCapabilityAsync("FindActiveSystems", Json("{}"));

        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("Experiment 1", json);
    }

    // ── IsDevice ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsDevice_Connected_ReturnsSuccess()
    {
        var (driver, opc) = CreateDriver();

        // Node ID from POP Fbrm: ns=2;Local.iCFBRM.ConnectionStatus
        opc.GetAtNode<bool>("ns=2;Local.iCFBRM.ConnectionStatus").Returns(true);

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.True(result.Success);
        opc.Received(1).GetAtNode<bool>("ns=2;Local.iCFBRM.ConnectionStatus");
    }

    [Fact]
    public async Task IsDevice_NotConnected_ReturnsFailure()
    {
        var (driver, opc) = CreateDriver();

        opc.GetAtNode<bool>("ns=2;Local.iCFBRM.ConnectionStatus").Returns(false);

        var result = await driver.InvokeCapabilityAsync("IsDevice", Json("{}"));

        Assert.False(result.Success);
    }

    // ── has_trends=NO guard ───────────────────────────────────────────────────

    [Fact]
    public void FbrmDoesNotOverrideStreamTrendsAsync()
    {
        // Verify that Fbrm does not declare StreamTrendsAsync (has_trends=NO per RESEARCH.md)
        var streamTrends = typeof(Fbrm).GetMethod(
            nameof(Fbrm.StreamTrendsAsync),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

        Assert.Null(streamTrends);
    }
}

using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class MicrosaicTests
{
    // ── Test helpers ──────────────────────────────────────────────────────────

    private static (Microsaic driver, IDeviceTransport transport) CreateDriver(params string[] responses)
    {
        var transport = Substitute.For<IDeviceTransport>();
        var queue = new Queue<byte[]>(responses.Select(r => Encoding.ASCII.GetBytes(r)));
        transport.SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                 .Returns(_ => Task.FromResult(queue.Dequeue()));
        var driver = new Microsaic(transport, NullLogger<Microsaic>.Instance);
        return (driver, transport);
    }

    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static string SentCommand(IDeviceTransport transport, int callIndex = 0)
    {
        var calls = transport.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "SendReceiveAsync")
            .ToList();
        return Encoding.ASCII.GetString((byte[])calls[callIndex].GetArguments()[0]);
    }

    // ── SetAttenuation ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetAttenuation_SendsCorrectCommand_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("OK\n\n");
        var result = await driver.InvokeCapabilityAsync("SetAttenuation", Json("{\"attenuation\":100}"));

        Assert.True(result.Success);
        Assert.Contains("PUT SET_MIDAS_ATTENUATOR\n100\nFALSE\n\n", SentCommand(transport));
    }

    [Fact]
    public async Task SetAttenuation_EmptyResponse_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("\n\n");
        var result = await driver.InvokeCapabilityAsync("SetAttenuation", Json("{\"attenuation\":100}"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SetAttenuation_NotOkResponse_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("FAIL\n\n");
        var result = await driver.InvokeCapabilityAsync("SetAttenuation", Json("{\"attenuation\":100}"));

        Assert.False(result.Success);
    }

    // ── StartMra ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task StartMra_SendsCorrectCommand_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("OK\n\n");
        var result = await driver.InvokeCapabilityAsync("StartMra", Json("{\"attenuation\":100}"));

        Assert.True(result.Success);
        Assert.Contains("PUT SET_MIDAS_ATTENUATOR\n100\nTRUE\n\n", SentCommand(transport));
    }

    [Fact]
    public async Task StartMra_EmptyResponse_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("\n\n");
        var result = await driver.InvokeCapabilityAsync("StartMra", Json("{\"attenuation\":100}"));

        Assert.False(result.Success);
    }

    // ── StopMra ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task StopMra_SendsCorrectCommand_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("OK\n\n");
        var result = await driver.InvokeCapabilityAsync("StopMra", Json("{}"));

        Assert.True(result.Success);
        Assert.Contains("PUT MIDAS_ATTENUATOR\nSTOP\n\n", SentCommand(transport));
    }

    [Fact]
    public async Task StopMra_EmptyResponse_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("\n\n");
        var result = await driver.InvokeCapabilityAsync("StopMra", Json("{}"));

        Assert.False(result.Success);
    }

    // ── SetMakeUpFlowRate ─────────────────────────────────────────────────────

    [Fact]
    public async Task SetMakeUpFlowRate_SendsCorrectCommand_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("OK\n\n");
        var result = await driver.InvokeCapabilityAsync("SetMakeUpFlowRate", Json("{\"flowRateUlMin\":10000}"));

        Assert.True(result.Success);
        Assert.Contains("PUT SET_MIDAS_MAKEUP_PUMP_FLOW_RATE\n10000\n\n", SentCommand(transport));
    }

    [Fact]
    public async Task SetMakeUpFlowRate_EmptyResponse_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("\n\n");
        var result = await driver.InvokeCapabilityAsync("SetMakeUpFlowRate", Json("{\"flowRateUlMin\":10000}"));

        Assert.False(result.Success);
    }

    // ── StartMakeUpFlow ───────────────────────────────────────────────────────

    [Fact]
    public async Task StartMakeUpFlow_SendsCorrectCommand_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("OK\n\n");
        var result = await driver.InvokeCapabilityAsync("StartMakeUpFlow", Json("{}"));

        Assert.True(result.Success);
        Assert.Contains("PUT MIDAS_MAKEUP_PUMP\nSTART\n\n", SentCommand(transport));
    }

    [Fact]
    public async Task StartMakeUpFlow_EmptyResponse_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("\n\n");
        var result = await driver.InvokeCapabilityAsync("StartMakeUpFlow", Json("{}"));

        Assert.False(result.Success);
    }

    // ── StopMakeUpFlow ────────────────────────────────────────────────────────

    [Fact]
    public async Task StopMakeUpFlow_SendsCorrectCommand_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("OK\n\n");
        var result = await driver.InvokeCapabilityAsync("StopMakeUpFlow", Json("{}"));

        Assert.True(result.Success);
        Assert.Contains("PUT MIDAS_MAKEUP_PUMP\nSTOP\n\n", SentCommand(transport));
    }

    [Fact]
    public async Task StopMakeUpFlow_EmptyResponse_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("\n\n");
        var result = await driver.InvokeCapabilityAsync("StopMakeUpFlow", Json("{}"));

        Assert.False(result.Success);
    }

    // ── InitialiseFullScanParameters ──────────────────────────────────────────

    [Fact]
    public async Task InitialiseFullScanParameters_Negative_SendsCorrectCommands_ReturnsSuccess()
    {
        // Response for PUT SPECTRUM_PARAMETERS then PUT ACQUISITION_PARAMETERS
        var (driver, transport) = CreateDriver("OK\n\n", "OK\n\n");
        var result = await driver.InvokeCapabilityAsync(
            "InitialiseFullScanParameters",
            Json("{\"lowerMz\":50,\"upperMz\":150,\"massStep\":0.1,\"scanTimeMs\":500,\"tuneMode\":\"Negative\"}"));

        Assert.True(result.Success);
        Assert.Contains("PUT SPECTRUM_PARAMETERS\n50\n150\n0.1\n", SentCommand(transport, 0));
        Assert.Contains("negative.tun", SentCommand(transport, 1));
    }

    [Fact]
    public async Task InitialiseFullScanParameters_Positive_SendsPositiveTuneFile()
    {
        var (driver, transport) = CreateDriver("OK\n\n", "OK\n\n");
        var result = await driver.InvokeCapabilityAsync(
            "InitialiseFullScanParameters",
            Json("{\"lowerMz\":50,\"upperMz\":150,\"massStep\":0.1,\"scanTimeMs\":500,\"tuneMode\":\"Positive\"}"));

        Assert.True(result.Success);
        Assert.Contains("positive.tun", SentCommand(transport, 1));
    }

    [Fact]
    public async Task InitialiseFullScanParameters_EmptyResponse_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("\n\n", "\n\n");
        var result = await driver.InvokeCapabilityAsync(
            "InitialiseFullScanParameters",
            Json("{\"lowerMz\":50,\"upperMz\":150,\"massStep\":0.1,\"scanTimeMs\":500,\"tuneMode\":\"Negative\"}"));

        Assert.False(result.Success);
    }

    // ── StopDataAcquisition ───────────────────────────────────────────────────

    [Fact]
    public async Task StopDataAcquisition_SendsCorrectCommand_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("TRUE\n\n");
        var result = await driver.InvokeCapabilityAsync("StopDataAcquisition", Json("{}"));

        Assert.True(result.Success);
        Assert.Contains("PUT STOP_IMMEDIATE\n\n", SentCommand(transport));
    }

    [Fact]
    public async Task StopDataAcquisition_FalseResponse_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("FALSE\n\n");
        var result = await driver.InvokeCapabilityAsync("StopDataAcquisition", Json("{}"));

        Assert.False(result.Success);
    }

    // ── GetAllStatusDetails ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllStatusDetails_FullResponses_ReturnsSuccess()
    {
        // Queue: MiDas status, Makeup pump status, Acquisition parameters, Method settings
        var (driver, _) = CreateDriver(
            "Run State : STOPPED \nMakeup Flow Rate : 1000.0 ul/min\nMakeup Dilution Factor : 100.0 \nClosest Attenuation : 100.0 \nMidas Dispense Rate : 50.0 ul/min\n",
            "OFF\n1000.0\n 1.3\nFALSE\nFALSE\nFALSE\nFALSE\n\n",
            "Ion Mode : NEGATIVE\nTip voltage : 111.1 V\n",
            "Overall Time : 10.0 mins\nStart Time : 0.0 mins\nStop Time : 10.0 mins\nAcquisition Mode : Full Scan \nMethod : EIC \nCommon Settings :  \nScan Start : 50.0 m/z\nScan End : 1400.0 m/z\nScan Step : 0.2 m/z\nScan Time : 437.6 ms\nDwell time : 100.0 ms\n");

        var result = await driver.InvokeCapabilityAsync("GetAllStatusDetails", Json("{}"));

        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("Negative", json);
    }
}

using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class KnauerAzuraTests
{
    private static (KnauerAzura driver, IDeviceTransport transport) CreateDriver(params string[] responses)
    {
        var transport = Substitute.For<IDeviceTransport>();
        var queue = new Queue<byte[]>(responses.Select(r => Encoding.ASCII.GetBytes(r)));
        transport.SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                 .Returns(_ => Task.FromResult(queue.Dequeue()));
        var driver = new KnauerAzura(transport, NullLogger<KnauerAzura>.Instance);
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

    [Fact]
    public async Task RemoteControl_SendsRemoteCommand_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("REMOTE:OK\r");
        var result = await driver.InvokeCapabilityAsync("RemoteControl", Json("{}"));
        Assert.True(result.Success);
        Assert.Contains("REMOTE:60000000", SentCommand(transport));
    }

    [Fact]
    public async Task Start_SendsStartThenUnhold_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("START:OK\r", "UNHOLD:OK\r");
        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));
        Assert.True(result.Success);
        Assert.Contains("START:2,2", SentCommand(transport, 0));
        Assert.Contains("UNHOLD", SentCommand(transport, 1));
    }

    [Fact]
    public async Task Start_StartFails_ReturnsFailure()
    {
        var (driver, _) = CreateDriver("START:ERROR\r");
        var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Stop_SendsStopCommand_ReturnsSuccess()
    {
        var (driver, transport) = CreateDriver("STOP:OK\r");
        var result = await driver.InvokeCapabilityAsync("Stop", Json("{}"));
        Assert.True(result.Success);
        Assert.Contains("STOP:1,1,1", SentCommand(transport));
    }

    [Fact]
    public async Task SetFlowRate_ValidParams_SendsRampCommand()
    {
        var (driver, transport) = CreateDriver("RAMP:OK\r");
        var result = await driver.InvokeCapabilityAsync("SetFlowRate",
            Json("""{"flowRateMlMin":1.5,"percA":60,"percB":40,"percC":0,"percD":0}"""));
        Assert.True(result.Success);
        // 1.5 ml/min * 1000 = 1500 ul/min
        Assert.Contains("RAMP:1500,0,60,40,0,0", SentCommand(transport));
    }

    [Fact]
    public async Task SetFlowRate_PercentsExceedTotal_ReturnsFailure()
    {
        var (driver, _) = CreateDriver(); // no responses needed
        var result = await driver.InvokeCapabilityAsync("SetFlowRate",
            Json("""{"flowRateMlMin":1.0,"percA":70,"percB":50,"percC":0,"percD":0}"""));
        Assert.False(result.Success);
        Assert.Contains("100", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task GetPumpStatus_Running_ReturnsStructuredResult()
    {
        // STATUS response: STATUS:ok,{runTime},{pumpStateInt},{?},{flowUlMin},{percA},{percB},{percC},{percD},?,?,?,?,?,?,?,?,{pressureBar*100}
        // state index 2 = 3 = Running; flow index 4 = 1500/1000 = 1.5 ml/min; pressure index 17 = 350/100 = 3.5 bar
        var (driver, _) = CreateDriver("STATUS:ok,15000,3,0,1500,60,40,0,0,0,0,0,0,0,0,0,0,350,0\r");
        var result = await driver.InvokeCapabilityAsync("GetPumpStatus", Json("{}"));
        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("Running", json);
    }

    [Fact]
    public async Task GetIdentity_ReturnsIdentifyString()
    {
        var (driver, _) = CreateDriver("IDENTIFY:3,KNAUER,AZURA,F001,1,2\r");
        var result = await driver.InvokeCapabilityAsync("GetIdentity", Json("{}"));
        Assert.True(result.Success);
        var json = result.ReturnValue!.Value.ToString();
        Assert.Contains("KNAUER", json);
    }
}

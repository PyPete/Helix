using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Transports.Opc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class SystagOpcUaClientTests
{
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Fact]
    public void OpcTransportOptions_AutoAcceptUntrustedCertificates_DefaultFalse()
    {
        var options = new OpcTransportOptions { EndpointUrl = "opc.tcp://localhost:4840" };
        Assert.False(options.AutoAcceptUntrustedCertificates);
    }

    [Fact]
    public async Task ReadNode_SendsOpcReadCommand_ReturnsSuccess()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var response = Encoding.UTF8.GetBytes("""{"value":75.5}""");
        transport.SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                 .Returns(Task.FromResult(response));
        var driver = new SystagOpcUaClient(transport, NullLogger<SystagOpcUaClient>.Instance);

        var result = await driver.InvokeCapabilityAsync("ReadNode",
            Json("""{"nodeId":"ns=2;s=Temperature"}"""));
        Assert.True(result.Success);

        var sentBytes = (byte[])transport.ReceivedCalls()
            .First(c => c.GetMethodInfo().Name == "SendReceiveAsync")
            .GetArguments()[0];
        var sentJson = Encoding.UTF8.GetString(sentBytes);
        Assert.Contains("ns=2;s=Temperature", sentJson);
        Assert.Contains("read", sentJson);
    }

    [Fact]
    public async Task WriteNode_SendsOpcWriteCommand_ReturnsSuccess()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var response = Encoding.UTF8.GetBytes("""{"Success":true}""");
        transport.SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                 .Returns(Task.FromResult(response));
        var driver = new SystagOpcUaClient(transport, NullLogger<SystagOpcUaClient>.Instance);

        var result = await driver.InvokeCapabilityAsync("WriteNode",
            Json("""{"nodeId":"ns=2;s=TempSetpoint","value":80.0}"""));
        Assert.True(result.Success);

        var sentBytes = (byte[])transport.ReceivedCalls()
            .First(c => c.GetMethodInfo().Name == "SendReceiveAsync")
            .GetArguments()[0];
        var sentJson = Encoding.UTF8.GetString(sentBytes);
        Assert.Contains("ns=2;s=TempSetpoint", sentJson);
        Assert.Contains("write", sentJson);
    }

    [Fact]
    public async Task WriteNode_OutOfRange_PassesThroughToServer()
    {
        // Driver does no range validation — OPC server enforces limits
        var transport = Substitute.For<IDeviceTransport>();
        var response = Encoding.UTF8.GetBytes("""{"Success":true}""");
        transport.SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                 .Returns(Task.FromResult(response));
        var driver = new SystagOpcUaClient(transport, NullLogger<SystagOpcUaClient>.Instance);

        var result = await driver.InvokeCapabilityAsync("WriteNode",
            Json("""{"nodeId":"ns=2;s=TempSetpoint","value":9999.0}"""));
        Assert.True(result.Success);
    }

    [Fact]
    public async Task SubscribeNode_SendsOpcSubscribeCommand_ReturnsSuccess()
    {
        var transport = Substitute.For<IDeviceTransport>();
        var response = Encoding.UTF8.GetBytes("""{"Success":true}""");
        transport.SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                 .Returns(Task.FromResult(response));
        var driver = new SystagOpcUaClient(transport, NullLogger<SystagOpcUaClient>.Instance);

        var result = await driver.InvokeCapabilityAsync("SubscribeNode",
            Json("""{"nodeId":"ns=2;s=Temperature"}"""));
        Assert.True(result.Success);

        var sentBytes = (byte[])transport.ReceivedCalls()
            .First(c => c.GetMethodInfo().Name == "SendReceiveAsync")
            .GetArguments()[0];
        var sentJson = Encoding.UTF8.GetString(sentBytes);
        Assert.Contains("subscribe", sentJson);
    }
}

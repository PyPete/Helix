using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Opc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Opc.Ua;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Transports;

public sealed class OpcTransportTests
{
    [Fact]
    public async Task SendReceiveAsync_Read_HappyPath_ReturnsNodeValue()
    {
        var session = Substitute.For<IOpcSession>();
        session.IsConnected.Returns(true);
        session.ReadNodeAsync<object>("ns=2;s=Temperature", Arg.Any<CancellationToken>())
            .Returns(42.5);

        var transport = CreateTransport(session);
        await transport.ConnectAsync();

        var command = JsonSerializer.SerializeToUtf8Bytes(new { NodeId = "ns=2;s=Temperature", Operation = "read" });
        var response = await transport.SendReceiveAsync(command);
        var json = Encoding.UTF8.GetString(response);

        Assert.Contains("42.5", json);
        await session.Received(1).ReadNodeAsync<object>("ns=2;s=Temperature", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendReceiveAsync_WhenSessionThrows_ThrowsTransportFaultException()
    {
        var session = Substitute.For<IOpcSession>();
        session.IsConnected.Returns(true);
        session.ReadNodeAsync<object>("ns=2;s=Pressure", Arg.Any<CancellationToken>())
            .Returns(_ => throw new ServiceResultException(StatusCodes.BadConnectionClosed));

        var transport = CreateTransport(session);
        await transport.ConnectAsync();

        var command = JsonSerializer.SerializeToUtf8Bytes(new { NodeId = "ns=2;s=Pressure", Operation = "read" });

        var exception = await Assert.ThrowsAsync<TransportFaultException>(() => transport.SendReceiveAsync(command));
        Assert.Equal("OPC UA", exception.TransportType);
    }

    [Fact]
    public async Task SendReceiveAsync_Subscribe_PublishesToDataReceived()
    {
        var session = Substitute.For<IOpcSession>();
        session.IsConnected.Returns(true);
        var subscription = Substitute.For<IOpcSubscription>();
        subscription.NodeId.Returns("ns=2;s=Temperature");
        subscription.IsActive.Returns(true);

        Action<object?>? callback = null;
        session.SubscribeNodeAsync(
                "ns=2;s=Temperature",
                Arg.Do<Action<object?>>(x => callback = x),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(subscription);

        var transport = CreateTransport(session);
        await transport.ConnectAsync();

        var nextPayload = transport.DataReceived.FirstAsync().ToTask();
        var command = JsonSerializer.SerializeToUtf8Bytes(new { NodeId = "ns=2;s=Temperature", Operation = "subscribe" });

        await transport.SendReceiveAsync(command);
        callback!(123.4);

        var payload = await nextPayload;
        var json = Encoding.UTF8.GetString(payload);
        Assert.Contains("ns=2;s=Temperature", json);
        Assert.Contains("123.4", json);
    }

    [Fact]
    public async Task ConnectAsync_WhenSessionInitiallyDisconnected_WatchdogTransitionsToReconnecting()
    {
        var session = Substitute.For<IOpcSession>();
        // First call returns true (connected), subsequent calls return false (disconnected)
        session.IsConnected.Returns(x => true, x => false);

        var transport = CreateTransport(session, reconnectPeriodMs: 50);
        await transport.ConnectAsync();

        // Wait for watchdog to detect disconnection (50ms reconnect period + margin)
        await Task.Delay(200);

        Assert.Equal(TransportStatus.Reconnecting, transport.Status);
    }

    private static OpcDeviceTransport CreateTransport(IOpcSession session, int reconnectPeriodMs = 5_000)
    {
        return new OpcDeviceTransport(
            new OpcTransportOptions
            {
                EndpointUrl = "opc.tcp://localhost:4840",
                ReconnectPeriodMs = reconnectPeriodMs
            },
            NullLogger<OpcDeviceTransport>.Instance,
            new FakeOpcSessionFactory(session));
    }

    private sealed class FakeOpcSessionFactory : IOpcSessionFactory
    {
        private readonly IOpcSession _session;

        public FakeOpcSessionFactory(IOpcSession session)
        {
            _session = session;
        }

        public IOpcSession Create(OpcTransportOptions options, ApplicationConfiguration applicationConfiguration) => _session;
    }
}

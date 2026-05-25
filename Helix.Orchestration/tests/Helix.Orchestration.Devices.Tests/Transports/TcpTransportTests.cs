using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Tcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Transports;

public sealed class TcpTransportTests
{
    [Fact]
    public async Task SendReceiveAsync_WithDelimiterFraming_ReturnsResponse()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            var buffer = new byte[64];
            var read = await stream.ReadAsync(buffer);
            var request = Encoding.ASCII.GetString(buffer, 0, read);
            Assert.Equal("PING\n", request);
            await stream.WriteAsync(Encoding.ASCII.GetBytes("PONG\n"));
        });

        await using var transport = new TcpDeviceTransport(
            new TcpTransportOptions
            {
                Host = "127.0.0.1",
                Port = port,
                FramingMode = TcpFramingMode.Delimiter,
                DelimiterByte = (byte)'\n'
            },
            NullLogger<TcpDeviceTransport>.Instance);

        await transport.ConnectAsync();
        var response = await transport.SendReceiveAsync(Encoding.ASCII.GetBytes("PING\n"));

        Assert.Equal("PONG\n", Encoding.ASCII.GetString(response));
        await serverTask;
    }

    [Fact]
    public async Task ReadLoop_WhenServerDropsConnection_SetsFaulted()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            client.Close();
        });

        await using var transport = new TcpDeviceTransport(
            new TcpTransportOptions
            {
                Host = "127.0.0.1",
                Port = port,
                FramingMode = TcpFramingMode.Raw
            },
            NullLogger<TcpDeviceTransport>.Instance);

        await transport.ConnectAsync();
        await Task.Delay(150);

        Assert.Equal(TransportStatus.Faulted, transport.Status);
        await serverTask;
    }

    [Fact]
    public async Task DataReceived_PublishesServerPushMessages()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            await Task.Delay(50);
            await stream.WriteAsync(Encoding.ASCII.GetBytes("PUSH"));
        });

        await using var transport = new TcpDeviceTransport(
            new TcpTransportOptions
            {
                Host = "127.0.0.1",
                Port = port,
                FramingMode = TcpFramingMode.Raw
            },
            NullLogger<TcpDeviceTransport>.Instance);

        await transport.ConnectAsync();
        var payload = await transport.DataReceived.FirstAsync().ToTask();

        Assert.Equal("PUSH", Encoding.ASCII.GetString(payload));
        await serverTask;
    }
}

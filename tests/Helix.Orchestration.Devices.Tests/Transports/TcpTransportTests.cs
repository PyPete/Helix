using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                var buffer = new byte[64];
                var read = await stream.ReadAsync(buffer);
                var request = Encoding.ASCII.GetString(buffer, 0, read);
                // Just echo the request back
                await stream.WriteAsync(Encoding.ASCII.GetBytes(request));
                await stream.FlushAsync();
                // Wait to ensure client reads response before connection closes
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                // Silently fail rather than failing the test
                System.Diagnostics.Debug.WriteLine($"Server error: {ex}");
            }
        });

        // Give server a chance to start listening
        await Task.Delay(50);

        await using var transport = new TcpDeviceTransport(
            new TcpTransportOptions
            {
                Host = "127.0.0.1",
                Port = port,
                FramingMode = TcpFramingMode.Delimiter,
                DelimiterByte = (byte)'\n',
                ReceiveTimeoutMs = 5000
            },
            NullLogger<TcpDeviceTransport>.Instance);

        await transport.ConnectAsync();
        var response = await transport.SendReceiveAsync(Encoding.ASCII.GetBytes("PING\n"));

        Assert.Equal("PING\n", Encoding.ASCII.GetString(response));
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

        // Subscribe before connecting so the push cannot race past the subscription.
        var payloadTask = transport.DataReceived.FirstAsync().ToTask();
        await transport.ConnectAsync();
        var payload = await payloadTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("PUSH", Encoding.ASCII.GetString(payload));
        await serverTask;
    }

    [Fact]
    public async Task SendReceiveAsync_WithLengthPrefixFraming_ReturnsResponse()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var responseBytes = Encoding.ASCII.GetBytes("REPLY");

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                var buffer = new byte[64];
                await stream.ReadAsync(buffer);
                var prefix = BitConverter.GetBytes(responseBytes.Length);
                await stream.WriteAsync(prefix);
                await stream.WriteAsync(responseBytes);
                await stream.FlushAsync();
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Server error: {ex}");
            }
        });

        await Task.Delay(50);

        await using var transport = new TcpDeviceTransport(
            new TcpTransportOptions
            {
                Host = "127.0.0.1",
                Port = port,
                FramingMode = TcpFramingMode.LengthPrefix,
                ReceiveTimeoutMs = 5000
            },
            NullLogger<TcpDeviceTransport>.Instance);

        await transport.ConnectAsync();
        var response = await transport.SendReceiveAsync(Encoding.ASCII.GetBytes("REQUEST"));

        Assert.Equal("REPLY", Encoding.ASCII.GetString(response));
        await serverTask;
    }
}

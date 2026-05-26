using System.Net.Sockets;
using Helix.Orchestration.Devices.Transport;

namespace Helix.Orchestration.Agent.Transport;

public sealed class TcpTransport : ITcpTransport, IDisposable
{
    private readonly TcpClient _client = new();

    public int SendTimeout
    {
        get => _client.SendTimeout;
        set => _client.SendTimeout = value;
    }

    public int ReceiveTimeout
    {
        get => _client.ReceiveTimeout;
        set => _client.ReceiveTimeout = value;
    }

    public bool Connected => _client.Connected;

    public async ValueTask ConnectAsync(string address, int port, CancellationToken cancellationToken = default)
        => await _client.ConnectAsync(address, port, cancellationToken);

    public NetworkStream GetStream() => _client.GetStream();

    public void Close() => _client.Close();

    public void Dispose() => _client.Dispose();
}

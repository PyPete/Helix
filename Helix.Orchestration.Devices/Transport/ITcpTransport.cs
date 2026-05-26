using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Helix.Orchestration.Devices.Transport;

/// <summary>
/// Abstracts TCP communications for device drivers.
/// </summary>
public interface ITcpTransport
{
    int SendTimeout { get; set; }

    int ReceiveTimeout { get; set; }

    bool Connected { get; }

    ValueTask ConnectAsync(string address, int port, CancellationToken cancellationToken = default);

    NetworkStream GetStream();

    void Close();
}

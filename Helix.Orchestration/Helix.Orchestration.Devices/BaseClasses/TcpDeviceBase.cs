using Helix.Orchestration.Devices.Transport;

namespace Helix.Orchestration.Devices.BaseClasses;

public abstract class TcpDeviceBase(ITcpTransport transport, string? name = null) : DeviceBase(name)
{
    protected ITcpTransport Transport { get; } = transport;

    protected string? Address { get; private set; }

    protected int Port { get; private set; }

    public virtual async ValueTask<bool> OpenAsync(string address, int port, CancellationToken cancellationToken = default)
    {
        try
        {
            await Transport.ConnectAsync(address, port, cancellationToken);
            Address = address;
            Port = port;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override Model.DeviceResult Close()
    {
        try
        {
            Transport.Close();
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Failed to close TCP transport: {ex.Message}");
        }
    }

    protected override IReadOnlyList<Model.ConnectionComponent> GetConnectionDetails() =>
    [
        new("Connection Type", "TCP"),
        new("Address", Address ?? string.Empty),
        new("Port", Port == 0 ? string.Empty : Port.ToString())
    ];
}

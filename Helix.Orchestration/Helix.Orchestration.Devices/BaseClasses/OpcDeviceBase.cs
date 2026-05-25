using Helix.Orchestration.Devices.Model;
using Helix.Orchestration.Devices.Transport;

namespace Helix.Orchestration.Devices.BaseClasses;

public abstract class OpcDeviceBase(IOpcTransport transport, string? name = null) : DeviceBase(name)
{
    protected IOpcTransport Transport { get; } = transport;

    protected string? Endpoint { get; private set; }

    public virtual DeviceResult Open(string endpoint)
    {
        var (success, reason) = Transport.Connect(endpoint);
        if (!success)
            return Failure(reason);

        Endpoint = endpoint;
        return Success();
    }

    public override DeviceResult Close()
    {
        try
        {
            Transport.Disconnect();
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Failed to close OPC transport: {ex.Message}");
        }
    }

    protected override IReadOnlyList<ConnectionComponent> GetConnectionDetails() =>
    [
        new("Connection Type", "OPC UA"),
        new("Endpoint", Endpoint ?? string.Empty)
    ];
}

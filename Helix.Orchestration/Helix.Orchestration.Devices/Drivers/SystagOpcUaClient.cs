using Helix.Orchestration.Devices.BaseClasses;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Helix.Orchestration.Devices.Transport;

namespace Helix.Orchestration.Devices.Drivers;

public sealed class SystagOpcUaClient(IOpcTransport transport, string? name = null)
    : OpcDeviceBase(transport, name), IReactionPlatform
{
    private readonly Dictionary<string, string> _measurementNames = new(StringComparer.OrdinalIgnoreCase);

    private string _activeSystem = string.Empty;

    public override DeviceKind DeviceKind => DeviceKind.ReactionPlatform;

    public DeviceResult CreateSubscription(string reactorNode)
    {
        try
        {
            var childNodes = Transport.BrowseChildren(reactorNode).ToArray();
            foreach (var node in childNodes)
            {
                var normalized = NormalizeNodeName(node);
                _measurementNames[normalized] = normalized;
            }

            _activeSystem = reactorNode;
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Failed creating OPC subscription projection: {ex.Message}");
        }
    }

    public override DeviceResult IsDevice()
        => Transport.IsConnected ? Success() : Failure("OPC transport is not connected.");

    public override (DeviceResult Result, string UniqueIdentifier) GetUniqueId()
    {
        if (string.IsNullOrWhiteSpace(_activeSystem))
            return (Warning("Active OPC reactor system not set."), string.Empty);

        return (Success(), $"systag-opc:{_activeSystem}");
    }

    public override IReadOnlyList<DeviceMeasurement> GetMeasurements()
    {
        if (!Transport.IsConnected || string.IsNullOrWhiteSpace(_activeSystem))
            return [];

        var timestamp = DateTime.UtcNow;
        var measurements = new List<DeviceMeasurement>();

        foreach (var measurement in _measurementNames.Keys)
        {
            var node = $"{_activeSystem}/{measurement}";
            try
            {
                var value = Transport.Read<object>(node);
                measurements.Add(new DeviceMeasurement(measurement, measurement, value, string.Empty, timestamp));
            }
            catch
            {
                // keep collection resilient per-node
            }
        }

        return measurements;
    }

    private static string NormalizeNodeName(string node)
        => node.Replace(" ", string.Empty, StringComparison.Ordinal);
}

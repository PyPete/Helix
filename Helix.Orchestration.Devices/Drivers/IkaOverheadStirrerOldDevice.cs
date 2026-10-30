using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the IKA Overhead Stirrer (old device variant).
/// Uses 7 data bits and Even parity — otherwise identical to IkaOverheadStirrer.
/// DI registration must set Parity=Even, DataBits=7.
/// </summary>
public sealed class IkaOverheadStirrerOldDevice : IkaOverheadStirrer
{
    public IkaOverheadStirrerOldDevice(SerialDeviceTransport transport, ILogger<IkaOverheadStirrerOldDevice> logger)
        : base(transport, logger) { }
}

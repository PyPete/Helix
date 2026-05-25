using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.Interfaces;

public interface IHplc : IDevice
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<DeviceResult> PrimeAsync(CancellationToken cancellationToken = default);

    Task<DeviceResult> StartMethodAsync(CancellationToken cancellationToken = default);
}

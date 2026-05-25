using System.Text;
using Helix.Orchestration.Devices.BaseClasses;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Helix.Orchestration.Devices.Transport;

namespace Helix.Orchestration.Devices.Drivers;

public sealed class KnauerAzura(ITcpTransport transport, string? name = null)
    : TcpDeviceBase(transport, name), IHplc
{
    private bool _initialized;

    public override DeviceKind DeviceKind => DeviceKind.Hplc;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!Transport.Connected)
            throw new InvalidOperationException("TCP transport is not connected. Call OpenAsync before initializing.");

        await SendCommandAsync("SYSTEM:INIT", cancellationToken);
        _initialized = true;
    }

    public async Task<DeviceResult> PrimeAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            return Failure("HPLC is not initialized.");

        try
        {
            await SendCommandAsync("PUMP:PRIME", cancellationToken);
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Prime failed: {ex.Message}");
        }
    }

    public async Task<DeviceResult> StartMethodAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            return Failure("HPLC is not initialized.");

        try
        {
            await SendCommandAsync("METHOD:START", cancellationToken);
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Start method failed: {ex.Message}");
        }
    }

    public override DeviceResult IsDevice()
    {
        if (!Transport.Connected)
            return Failure("TCP transport is not connected.");

        return Success();
    }

    public override (DeviceResult Result, string UniqueIdentifier) GetUniqueId()
    {
        if (!Transport.Connected)
            return (Failure("TCP transport is not connected."), string.Empty);

        return (Success(), $"knauer-azura:{Name}");
    }

    public override IReadOnlyList<DeviceMeasurement> GetMeasurements() =>
    [
        new("HplcInitialized", "HplcInitialized", _initialized, "bool", DateTime.UtcNow)
    ];

    private async Task SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        var stream = Transport.GetStream();
        var bytes = Encoding.ASCII.GetBytes(command + "\n");
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}

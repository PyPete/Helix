using System.Threading;
using Helix.Orchestration.Devices.Model;
using Helix.Orchestration.Devices.Transport;

namespace Helix.Orchestration.Devices.BaseClasses;

public abstract class SerialDeviceBase(ISerialTransport transport, string? name = null) : DeviceBase(name)
{
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    protected ISerialTransport Transport { get; } = transport;

    protected abstract int BaudRate { get; }

    public virtual bool Open(string portName)
    {
        try
        {
            Transport.Open(portName, BaudRate);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override DeviceResult Close()
    {
        try
        {
            Transport.Close();
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Failed to close serial transport: {ex.Message}");
        }
    }

    protected string SendCommand(string command, int timeoutMs = 200)
    {
        _commandLock.Wait();
        try
        {
            return Transport.SendCommand(command, timeoutMs);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    protected string SendCommand(byte[] command, int timeoutMs = 200)
    {
        _commandLock.Wait();
        try
        {
            return Transport.SendCommand(command, timeoutMs);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    protected byte[] SendCommandFixedResponse(byte[] command, int expectedBytes, int timeoutMs = 200)
    {
        _commandLock.Wait();
        try
        {
            return Transport.SendCommandFixedResponse(command, expectedBytes, timeoutMs);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    protected override IReadOnlyList<ConnectionComponent> GetConnectionDetails() =>
    [
        new("Connection Type", "Serial"),
        new("Baud Rate", BaudRate.ToString())
    ];
}

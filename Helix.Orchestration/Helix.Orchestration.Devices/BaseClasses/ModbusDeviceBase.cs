using Helix.Orchestration.Devices.Model;
using Helix.Orchestration.Devices.Transport;

namespace Helix.Orchestration.Devices.BaseClasses;

public abstract class ModbusDeviceBase(IModbusTransport transport, string? name = null) : DeviceBase(name)
{
    protected IModbusTransport Transport { get; } = transport;

    protected virtual int BaudRate => 9600;

    protected string? PortName { get; private set; }

    public virtual bool Open(string portName, int? baudRate = null)
    {
        try
        {
            Transport.Open(portName, baudRate ?? BaudRate);
            PortName = portName;
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected ReadOnlySpan<byte> Read(byte slaveId, ushort address, ushort quantity)
        => Transport.Read(slaveId, address, quantity);

    public override DeviceResult Close()
    {
        try
        {
            Transport.Close();
            return Success();
        }
        catch (Exception ex)
        {
            return Failure($"Failed to close Modbus transport: {ex.Message}");
        }
    }

    protected override IReadOnlyList<ConnectionComponent> GetConnectionDetails() =>
    [
        new("Connection Type", "Modbus RTU"),
        new("Port", PortName ?? string.Empty),
        new("Baud Rate", BaudRate.ToString())
    ];
}

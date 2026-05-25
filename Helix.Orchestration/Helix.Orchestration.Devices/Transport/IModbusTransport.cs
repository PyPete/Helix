namespace Helix.Orchestration.Devices.Transport;

/// <summary>
/// Abstracts Modbus RTU communication (read coils/registers).
/// </summary>
public interface IModbusTransport
{
    bool IsOpen { get; }

    void Open(string portName, int baudRate = 9600);

    void Close();

    /// <summary>Reads <paramref name="quantity"/> registers from <paramref name="slaveId"/> starting at <paramref name="address"/>.</summary>
    ReadOnlySpan<byte> Read(byte slaveId, ushort address, ushort quantity);
}

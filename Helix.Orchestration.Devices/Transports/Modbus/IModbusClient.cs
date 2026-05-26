using FluentModbus;

namespace Helix.Orchestration.Devices.Transports.Modbus;

/// <summary>
/// Testable abstraction over the FluentModbus RTU client.
/// </summary>
public interface IModbusClient : IDisposable
{
    void Connect(string portName, ModbusEndianness endianness);

    void Close();

    byte[] ReadHoldingRegisters(byte slaveId, ushort address, ushort quantity);

    byte[] ReadInputRegisters(byte slaveId, ushort address, ushort quantity);

    void WriteMultipleRegisters(byte slaveId, ushort address, byte[] data);

    byte[] ReadCoils(byte slaveId, ushort address, ushort quantity);
}

/// <summary>
/// Factory for creating Modbus clients.
/// </summary>
public interface IModbusClientFactory
{
    IModbusClient Create(ModbusTransportOptions options);
}

internal sealed class ModbusClientFactory : IModbusClientFactory
{
    public IModbusClient Create(ModbusTransportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var client = new ModbusRtuClient
        {
            BaudRate = options.BaudRate,
            ReadTimeout = options.ReadTimeoutMs,
            WriteTimeout = options.WriteTimeoutMs
        };

        return new ModbusClient(client);
    }
}

internal sealed class ModbusClient : IModbusClient
{
    private readonly ModbusRtuClient _client;

    public ModbusClient(ModbusRtuClient client)
    {
        _client = client;
    }

    public void Connect(string portName, ModbusEndianness endianness) => _client.Connect(portName, endianness);

    public void Close() => _client.Close();

    public byte[] ReadHoldingRegisters(byte slaveId, ushort address, ushort quantity) =>
        _client.ReadHoldingRegisters<byte>(slaveId, address, quantity).ToArray();

    public byte[] ReadInputRegisters(byte slaveId, ushort address, ushort quantity) =>
        _client.ReadInputRegisters<byte>(slaveId, address, quantity).ToArray();

    public void WriteMultipleRegisters(byte slaveId, ushort address, byte[] data) =>
        _client.WriteMultipleRegisters(slaveId, address, data);

    public byte[] ReadCoils(byte slaveId, ushort address, ushort quantity) =>
        _client.ReadCoils(slaveId, address, quantity).ToArray();

    public void Dispose() => _client.Dispose();
}

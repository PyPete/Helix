using FluentModbus;
using Helix.Orchestration.Devices.Transport;

namespace Helix.Orchestration.Agent.Transport;

public sealed class ModbusTransport : IModbusTransport, IDisposable
{
    private ModbusRtuClient _client = new();

    public bool IsOpen => _client.IsConnected;

    public void Open(string portName, int baudRate = 9600)
    {
        if (_client.IsConnected)
            Close();

        _client = new ModbusRtuClient
        {
            BaudRate = baudRate
        };

        _client.Connect(portName, ModbusEndianness.BigEndian);
    }

    public void Close()
    {
        _client.Dispose();
        _client = new ModbusRtuClient();
    }

    public ReadOnlySpan<byte> Read(byte slaveId, ushort address, ushort quantity)
    {
        var registers = _client.ReadHoldingRegisters<byte>(slaveId, address, quantity);
        return registers;
    }

    public void Dispose()
    {
        Close();
        _client.Dispose();
    }
}

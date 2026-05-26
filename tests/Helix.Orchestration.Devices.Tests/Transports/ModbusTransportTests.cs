using System.Text;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Modbus;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Transports;

public sealed class ModbusTransportTests
{
    [Fact]
    public async Task ReadHoldingRegistersAsync_ReturnsBytesFromClient()
    {
        var client = new FakeModbusClient
        {
            HoldingRegistersResult = [0x01, 0x02, 0x03, 0x04]
        };
        var transport = CreateTransport(client, slaveId: 7);

        await transport.ConnectAsync();
        var result = await transport.ReadHoldingRegistersAsync(100, 4);

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, result);
        Assert.Equal((byte)7, client.LastSlaveId);
        Assert.Equal((ushort)100, client.LastAddress);
        Assert.Equal((ushort)4, client.LastQuantity);
    }

    [Fact]
    public async Task ReadHoldingRegistersAsync_WhenIOException_SetsFaulted()
    {
        var client = new FakeModbusClient
        {
            ReadHoldingException = new IOException("port dropped")
        };
        var transport = CreateTransport(client);

        await transport.ConnectAsync();

        var ex = await Assert.ThrowsAsync<TransportFaultException>(() => transport.ReadHoldingRegistersAsync(0, 2));

        Assert.Equal("Modbus RTU", ex.TransportType);
        Assert.Equal(TransportStatus.Faulted, transport.Status);
    }

    [Fact]
    public async Task ReadHoldingRegistersAsync_UsesConfiguredSlaveId()
    {
        var client = new FakeModbusClient
        {
            HoldingRegistersResult = [0xAA]
        };
        var transport = CreateTransport(client, slaveId: 42);

        await transport.ConnectAsync();
        await transport.ReadHoldingRegistersAsync(12, 1);

        Assert.Equal((byte)42, client.LastSlaveId);
    }

    private static ModbusDeviceTransport CreateTransport(FakeModbusClient client, byte slaveId = 1)
    {
        return new ModbusDeviceTransport(
            new ModbusTransportOptions
            {
                Mode = ModbusMode.Rtu,
                PortName = "COM9",
                SlaveId = slaveId
            },
            NullLogger<ModbusDeviceTransport>.Instance,
            new FakeModbusClientFactory(client));
    }

    private sealed class FakeModbusClientFactory : IModbusClientFactory
    {
        private readonly IModbusClient _client;

        public FakeModbusClientFactory(IModbusClient client)
        {
            _client = client;
        }

        public IModbusClient Create(ModbusTransportOptions options) => _client;
    }

    private sealed class FakeModbusClient : IModbusClient
    {
        public byte[] HoldingRegistersResult { get; set; } = [];
        public byte[] InputRegistersResult { get; set; } = [];
        public byte[] CoilsResult { get; set; } = [];
        public Exception? ReadHoldingException { get; set; }
        public Exception? ReadInputException { get; set; }
        public Exception? ReadCoilsException { get; set; }
        public Exception? WriteException { get; set; }

        public byte LastSlaveId { get; private set; }
        public ushort LastAddress { get; private set; }
        public ushort LastQuantity { get; private set; }
        public byte[]? LastWriteData { get; private set; }
        public bool Connected { get; private set; }

        public void Connect(string portName, FluentModbus.ModbusEndianness endianness)
        {
            Connected = true;
        }

        public void Close()
        {
            Connected = false;
        }

        public byte[] ReadHoldingRegisters(byte slaveId, ushort address, ushort quantity)
        {
            LastSlaveId = slaveId;
            LastAddress = address;
            LastQuantity = quantity;

            if (ReadHoldingException is not null)
            {
                throw ReadHoldingException;
            }

            return HoldingRegistersResult;
        }

        public byte[] ReadInputRegisters(byte slaveId, ushort address, ushort quantity)
        {
            LastSlaveId = slaveId;
            LastAddress = address;
            LastQuantity = quantity;

            if (ReadInputException is not null)
            {
                throw ReadInputException;
            }

            return InputRegistersResult;
        }

        public void WriteMultipleRegisters(byte slaveId, ushort address, byte[] data)
        {
            LastSlaveId = slaveId;
            LastAddress = address;
            LastWriteData = data;

            if (WriteException is not null)
            {
                throw WriteException;
            }
        }

        public byte[] ReadCoils(byte slaveId, ushort address, ushort quantity)
        {
            LastSlaveId = slaveId;
            LastAddress = address;
            LastQuantity = quantity;

            if (ReadCoilsException is not null)
            {
                throw ReadCoilsException;
            }

            return CoilsResult;
        }

        public void Dispose()
        {
            Connected = false;
        }
    }
}

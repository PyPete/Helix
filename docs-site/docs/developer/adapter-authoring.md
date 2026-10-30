---
id: adapter-authoring
title: Adapter Authoring Guide
sidebar_position: 3
---

# Adapter Authoring Guide

This guide walks you through writing a new device adapter for the Helix Orchestration platform.
By the end you will have a compilable stub driver that the CapabilityRegistry can discover.
The VapourtecR4 adapter is used as a worked example throughout.

## What is a Device Adapter?

A device adapter bridges a physical instrument to the Helix capability model.
Workflows call named capabilities through ICapabilityInvoker, which dispatches to the adapter.

## Prerequisites

- Reference Helix.Orchestration.Devices in your project.
- Inherit from DeviceAdapterBase.
- Register using `AddHelixSerialAdapter<MyAdapter>`.

## Step-by-Step Guide

### Step 1: Create the class

```csharp
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace MyCompany.Devices.Drivers;

public sealed class MyInstrument : DeviceAdapterBase
{
    public MyInstrument(SerialDeviceTransport transport, ILogger<MyInstrument> logger)
        : base(transport, logger) { }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Capability registrations go here
    }
}
```

### Step 2: Choose a transport

| Transport class | Use when |
|-----------------|----------|
| SerialDeviceTransport | RS-232 / RS-485 |
| TcpDeviceTransport | Raw TCP socket |
| ModbusDeviceTransport | Modbus RTU |
| OpcDeviceTransport | OPC UA |
| CDllDeviceTransport | Native Windows DLL |

### Step 3: Implement capabilities

```csharp
builder.RegisterSimple(
    "GetFirmwareVersion",
    "Query the firmware version",
    async ct =>
    {
        var resp = Decode(await Transport.SendReceiveAsync(Encode("GV\r\n"), ct));
        if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
            return CapabilityInvocationResult.Failed(resp);
        return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
    });
```

### Step 4: Register in DI

```csharp
services.AddHelixSerialAdapter<MyInstrument>(options =>
{
    options.PortName = "COM3";
    options.BaudRate = 9600;
    options.CommandTerminator = "\r\n";
});
```

### Step 5: Write a unit test

```csharp
[Fact]
public async Task SetTemperature_SendsCorrectCommand()
{
    var transport = Substitute.For<IDeviceTransport>();
    transport.Status.Returns(TransportStatus.Connected);
    var adapter = new MyInstrument(transport, Substitute.For<ILogger<MyInstrument>>());
    var result = await adapter.InvokeCapabilityAsync("SetTemperature", ...);
    Assert.True(result.IsSuccess);
}
```
---

## VapourtecR4 Worked Example

The VapourtecR4 adapter (VapourtecR4.cs) communicates over SerialDeviceTransport
using the Vapourtec ASCII protocol.

### Class declaration

```csharp
public sealed class VapourtecR4 : DeviceAdapterBase
{
    public VapourtecR4(SerialDeviceTransport transport, ILogger<VapourtecR4> logger)
        : base(transport, logger) { }
}
```

Note that DeviceAdapterBase takes IDeviceTransport -- SerialDeviceTransport is injected
concretely here so DI can resolve the correct Singleton instance.

### Capabilities (sourced from VapourtecR4.cs)

| Capability | Parameters | Description |
|------------|-----------|-------------|
| Start | none | Start R4 pumps (PN CR LF then FW PN CR LF) |
| Stop | none | Stop R4 pumps (PF CR LF then FW PF CR LF) |
| SetFlowRate | pump (0-3), rateMlMin (0.0-9.99) | Set pump channel flow rate |
| SetTemperature | reactor (int), targetCelsius (0-150) | Set reactor target temperature |
| SelectReagent | channel (0-3) | Select reagent valve position |
| SelectSolvent | channel (0-3) | Select solvent valve position |
| SelectLoopFill | loop (0-3) | Select loop fill valve position |
| SelectLoopInject | loop (0-3) | Select loop inject valve position |
| GetFirmwareVersion | none | Query firmware version string |
| GetTemperatures | none | Query reactor temperatures A-D |

### Example: SetTemperature

The command string is R4 ST \{reactor\} \{targetCelsius\} CR LF:

```csharp
var cmd = $"R4 ST {reactor} {targetCelsius} \r\n";
var resp = Decode(await Transport.SendReceiveAsync(Encode(cmd), ct));
if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
    return CapabilityInvocationResult.Failed(resp);
if (!resp.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
    return CapabilityInvocationResult.Failed(resp);
return CapabilityInvocationResult.Successful();
```

### Example: SetFlowRate pump channel routing

Pumps 0-1 use the primary board; pumps 2-3 use the FW board. Rate x 1000 = ul/min:

```csharp
var rateInt = (int)(rateMlMin * 1000);
string cmd;
if (pump <= 1)
    cmd = $"FR {pump} {rateInt} \r\n";
else
    cmd = $"FW FR {pump - 2} {rateInt} \r\n";
```

### Trend streaming

StreamTrendsAsync polls every 2 seconds.
HT CR LF returns temperatures A-D (raw / 10 = Celsius).
HF CR LF returns pump A and B flow rates (raw / 1000 = ml/min).
Trend names: vapourtec.temperature.A, vapourtec.temperature.B,
vapourtec.flow.pumpA, vapourtec.flow.pumpB.
---

## Stub Driver Template

```csharp
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace MyCompany.Devices.Drivers;

public sealed class MyInstrument : DeviceAdapterBase
{
    public MyInstrument(SerialDeviceTransport transport, ILogger<MyInstrument> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string s) => System.Text.Encoding.ASCII.GetBytes(s);
    private static string Decode(byte[] b) => System.Text.Encoding.ASCII.GetString(b).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        builder.RegisterSimple(
            "GetStatus",
            "Query instrument status",
            async ct =>
            {
                var resp = Decode(await Transport.SendReceiveAsync(Encode("ST?\r\n"), ct));
                return resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase)
                    ? CapabilityInvocationResult.Failed(resp)
                    : CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
            });
    }
}
```

DI registration:

```csharp
builder.Services.AddHelixSerialAdapter<MyInstrument>(options =>
{
    options.PortName = "COM4";
    options.BaudRate = 9600;
    options.CommandTerminator = "\r\n";
});
```

---

## Common Pitfalls

### 1. Singleton transport lifetime

CapabilityRegistry is Singleton. Transports must also be Singleton.
The AddHelixAdapter helpers enforce this. Never register transports as Scoped or Transient.

### 2. IRunspaceEventEmitter nullable injection

Inject as nullable IRunspaceEventEmitter? and use null-conditional emitter?.Publish(...).

### 3. Transport fault handling

The transport throws TransportFaultException when not connected.

### 4. Error-first response check

Always check for ERR before OK. Some instruments return OKWITH_WARNING variants.

### 5. DeviceRegistry is not auto-populated

DeviceRegistry is Singleton but AddMigratedDeviceAdapters does not register it.

### 6. Trend names must be unique across adapters

Use a vendor-specific prefix to avoid MQTT topic collisions.
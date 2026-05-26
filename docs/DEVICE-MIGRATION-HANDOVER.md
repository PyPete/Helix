# Device Migration Handover — POP.DeviceCore → Helix.Orchestration.Devices

## Overview

This document captures the design decisions, architectural boundaries, and known parity gaps resulting from the migration of device drivers from the external `POP.DeviceCore` library into the Helix platform. It is intended as a reference for continuing the driver parity work and for understanding why the current architecture looks the way it does.

---

## Background

The source material is the `POP.DeviceCore` project from the `PopNextGen` solution. That repository contained:

- `Comm/` — concrete transport implementations tightly coupled to hardware SDKs (`System.IO.Ports`, `FluentModbus`, `TcpClient`, `Opc.UaFx.Advanced`)
- `Drivers/` — device-specific logic (`VapourtecR4`, `KnauerAzura`, `SystagOpcUaClient`, and others)
- `POP.Shared` — result/status types (`Info`, `Trend<T>`)
- `POP.Standards` — device interfaces and enums

The original code assumed a standalone desktop application with direct hardware access and no workflow engine. The migration strips that assumption entirely.

---

## Architecture Decision: Two-Layer Split

### Decision

The POP codebase was **not ported as-is**. Instead it was split into two explicit layers:

| Layer | Project | Responsibility |
|---|---|---|
| Pure device logic | `Helix.Orchestration.Devices` | Interfaces, base classes, driver implementations, transport contracts, domain models |
| Hardware transport | `Helix.Orchestration.Agent` | Concrete transport implementations (`SerialTransport`, `TcpTransport`, `ModbusTransport`, `OpcTransport`), capability adapters, DI registration |

### Rationale

Helix workflows must not reference hardware SDKs. Workflows dispatch capability invocations via `WorkflowContext.Capabilities`, which resolves to `ICapabilityInvoker`. Device implementations must be behind that boundary. By placing the transport contracts (`ISerialTransport`, `ITcpTransport`, `IModbusTransport`, `IOpcTransport`) in the Devices project and the concrete transport implementations in the Agent, drivers remain testable without hardware, and SDK upgrades or replacements require changes only in the Agent project.

This mirrors the existing Helix principle: the Agent tier owns external integration; the domain tier owns protocol logic.

---

## Transport Contracts

Each transport interface lives in `Helix.Orchestration.Devices/Transport/`:

### `ISerialTransport`

```
Open(portName)
Close()
IsOpen
SendCommand(string, int timeoutMs) → string
SendCommand(byte[], int timeoutMs) → string
SendCommandFixedResponse(byte[], int expectedBytes, int timeoutMs) → byte[]
```

Implemented in Agent by `SerialTransport` over `System.IO.Ports.SerialPort`.

### `ITcpTransport`

```
ConnectAsync(host, port, CancellationToken) → Task
Connected → bool
SendTimeout / ReceiveTimeout
GetStream() → NetworkStream
Close()
```

Implemented in Agent by `TcpTransport` over `System.Net.Sockets.TcpClient`.

### `IModbusTransport`

```
Open(portName, baudRate = 9600)
Close()
Read(slaveId, address, quantity) → ReadOnlySpan<short>
```

Implemented in Agent by `ModbusTransport` over `FluentModbus`.

### `IOpcTransport`

```
Connect(endpoint)
Disconnect()
IsConnected → bool
Read<T>(nodeId) → T
Write<T>(nodeId, value)
BrowseChildren(nodeId) → IEnumerable<string>
```

Implemented in Agent by `OpcTransport` over `Opc.UaFx.Advanced`.

### Concurrency

`SerialDeviceBase` owns a `SemaphoreSlim(1,1)` command lock. All `SendCommand` overloads serialize through it. This means drivers do not need their own locking — it is handled at the base class level. Higher-level drivers must not acquire additional locks around serial calls or deadlock will occur.

---

## Domain Model Replacements

POP types were replaced with Helix-native types to avoid any dependency on `POP.Shared` or `POP.Standards`:

| POP type | Helix replacement | Notes |
|---|---|---|
| `Info` | `DeviceResult` | `State` enum: `Success`, `Warning`, `Failure`; factory helpers `Ok()`, `Warn(string)`, `Fail(string)` |
| `Trend<T>` | `DeviceMeasurement` | Record; includes trend name, default name, value, unit, timestamp, optional reaction/X-axis metadata |
| `DeviceType` | `DeviceKind` | Enum with same categories: `ReactionPlatform`, `Hplc`, `Vapourtec`, `VacPump`, `OverheadStirrer`, etc. |
| `DeviceDetails` | `DeviceDetails` record | Composed from `DeviceInfoComponent`, `CommandComponent`, `ConnectionComponent` |

---

## Base Class Hierarchy

```
DeviceBase (abstract)
├── SerialDeviceBase (abstract) — adds ISerialTransport + SemaphoreSlim lock
│   └── VapourtecR4
├── TcpDeviceBase (abstract) — adds ITcpTransport + OpenAsync/Close
│   └── KnauerAzura
├── OpcDeviceBase (abstract) — adds IOpcTransport + Connect/Close
│   └── SystagOpcUaClient
├── ModbusDeviceBase (abstract) — adds IModbusTransport + read helpers
│   └── (future Modbus drivers)
├── SetPointDeviceBase (abstract) — standardises Start/Stop/SetPoint/Limits
│   └── StirrerDeviceBase
│   └── VacPumpDeviceBase
```

`DeviceBase` provides: name handling, measurement name registry, result factory helpers (`Success()`, `Failure()`, `Warning()`), metadata composition via `GetDeviceDetails()`, and abstract stubs for `IsDevice()`, `GetUniqueId()`, `GetMeasurements()`, `GetCommands()`, `GetConnectionDetails()`.

---

## Driver Status — Wave 1

The wave-1 ports are **architectural scaffolds**, not full behavioral parity ports. This was intentional for the first migration to establish the correct layer boundaries and prove the DI/capability wiring end-to-end. Parity work is a separate tracked concern.

### `VapourtecR4` — Scaffold (Parity Incomplete)

**Source:** `POP.DeviceCore/Drivers/VapourtecR4.cs`  
**Transport:** `ISerialTransport` via `SerialDeviceBase`  
**Baud rate:** 19200  
**Interface implemented:** `IVapourtec`

#### What is implemented

| Operation | Command sent | Notes |
|---|---|---|
| `IsDevice()` | `GV` | Firmware version check; any non-empty response is `Success` |
| `GetUniqueId()` | `GV` | Returns `vapourtec-r4:{firmware}` |
| `Start()` | `PN` | Power on |
| `Stop()` | `PF` | Power off |
| `SetFlowRate(pump, rate)` | `FR {pump} {rate:0.###}` | Caches last set value in `_pumpFlowRates` |
| `SetTemperature(reactor, target)` | `ST {reactor} {target}` | Caches last set value in `_reactorTemps` |
| `SelectReagent(channel)` | `KP {channel}` | Valve switch |
| `SelectSolvent(channel)` | `KP {channel}` | Same underlying command as SelectReagent — see gaps below |
| `GetMeasurements()` | — | Returns cached setpoint values only; no live read-back |

#### Known parity gaps vs. original POP implementation

1. **`SelectReagent` and `SelectSolvent` are the same command.** The original POP driver uses valve position encoding to distinguish reagent vs. solvent selection. The scaffold sends `KP {channel}` for both. The correct implementation must encode valve position per the R4 protocol documentation.

2. **No live measurement read-back.** `GetMeasurements()` returns only cached values that were set via `SetFlowRate`/`SetTemperature`. The original driver queries the device for actual flow rates and reactor temperatures using `GF`/`GT` poll commands. These poll commands are not yet implemented.

3. **No status polling loop.** The original driver exposes a `GetStatus()` or equivalent polling method to query run state, fault conditions, and valve positions. This is absent.

4. **No pump configuration.** The original driver has `SetPumpMode`, pump calibration parameters, and pressure-limit configuration. These are not in `IVapourtec` or the scaffold.

5. **No event/fault callbacks.** The original driver raises events on fault conditions. The scaffold has no fault notification path.

6. **Measurement units are hardcoded.** `uL/min` and `C` are embedded constants. The original driver sources units from device capability metadata.

7. **`BaudRate` is declared as an override but is not passed to transport.** `SerialDeviceBase.Open()` calls `Transport.Open(portName)` without passing `BaudRate`. The concrete `SerialTransport` uses its own internal baud rate defaulting. The `BaudRate` property on `SerialDeviceBase` is a placeholder — it must be wired through `ISerialTransport.Open(portName, baudRate)` when the transport contract is extended.

---

### `KnauerAzura` — Scaffold (Parity Incomplete)

**Source:** `POP.DeviceCore/Drivers/KnauerAzura.cs`  
**Transport:** `ITcpTransport` via `TcpDeviceBase`  
**Interface implemented:** `IHplc`

#### What is implemented

- `InitializeAsync`: sends `SYSTEM:INIT`, sets `_initialized = true`
- `PrimeAsync`: sends `PUMP:PRIME`, guarded by `_initialized`
- `StartMethodAsync`: sends `METHOD:START`, guarded by `_initialized`
- `IsDevice()`: checks `Transport.Connected`
- `GetUniqueId()`: returns `knauer-azura:{Name}`
- `GetMeasurements()`: returns a single `HplcInitialized` boolean measurement

#### Known parity gaps

1. **Command strings are assumed.** `SYSTEM:INIT`, `PUMP:PRIME`, `METHOD:START` are invented scaffolds. The actual Knauer Azura protocol uses a different command vocabulary that must be sourced from the Knauer SDK or protocol spec.

2. **No response validation.** `SendCommandAsync` writes to the stream but the response is not read or validated. Real Knauer commands have ACK/NAK responses.

3. **No gradient/solvent composition commands.** The original driver supports solvent mixing, gradient tables, and UV wavelength configuration.

4. **No run status or data acquisition.** No polling for run state, pressure, flow, or UV signal.

---

### `SystagOpcUaClient` — Scaffold (Parity Incomplete)

**Source:** `POP.DeviceCore/Drivers/SystagOpcUaClient.cs`  
**Transport:** `IOpcTransport` via `OpcDeviceBase`  
**Interface implemented:** `IReactionPlatform`

#### What is implemented

- `CreateSubscription(reactorNode)`: browses child nodes and builds a measurement name projection
- `IsDevice()`: checks `Transport.IsConnected`
- `GetUniqueId()`: returns `systag-opc:{activeSystem}`
- `GetMeasurements()`: reads all registered node values via `IOpcTransport.Read<object>`; resilient per-node (exceptions are swallowed)

#### Known parity gaps

1. **No write/setpoint methods.** The original Systag driver writes setpoints (temperature, stirrer speed) to OPC nodes. The scaffold only reads.

2. **Node path conventions are not enforced.** Node IDs are constructed as `{reactorNode}/{measurement}`, which is a simplification. Real Systag OPC trees have a richer address space structure.

3. **No change subscription.** `IOpcTransport` has no event/subscription API. The original driver uses OPC UA subscriptions with change notifications, not polling. The current polling approach in `GetMeasurements()` is a placeholder.

4. **`GetTimingLowerLimit()` not implemented.** Returns the `DeviceBase` default (0).

---

## Capability Adapter and DI Wiring

### `DeviceCapabilityAdapter<TDevice>`

A generic adapter that bridges any `IDevice` to `ICapabilityInvoker`. Each device instance is registered as a named capability:

```
device.vapourtec.r4       → DeviceCapabilityAdapter<VapourtecR4>
device.hplc.knauer.azura  → DeviceCapabilityAdapter<KnauerAzura>
device.reaction.systag    → DeviceCapabilityAdapter<SystagOpcUaClient>
```

The adapter's `Execute` dispatch handles three generic operations:

| Operation string | Delegates to |
|---|---|
| `"health"` | `IDevice.IsDevice()` |
| `"close"` | `IDevice.Close()` |
| `"measurements"` | `IDevice.GetMeasurements()` |
| _(null/empty)_ | `IDevice.GetMeasurements()` (default) |
| anything else | throws `NotSupportedException` |

Device-specific operations (e.g., `SetFlowRate`, `PrimeAsync`) are not yet dispatched through the generic adapter. To expose them, either:

- **Option A:** Extend the `Execute` switch to pattern-match on `TDevice` type and cast
- **Option B:** Create a typed `VapourtecCapabilityAdapter : ICapabilityInvoker` that natively handles `set-flow-rate`, `set-temperature`, etc.

Option B is preferred for complex devices. Option A is adequate for health/measurement-only access during parity development.

### Registration Lifecycle

Devices and transports are registered as `Transient` in DI. The `CapabilityRegistry` is a `Singleton` and is built eagerly by resolving device instances from the DI container at startup. This means the first device construction happens when the Agent starts — if a transport fails to open, the exception will surface at startup, not during the first invocation.

> **Important:** Because transports are `Transient` and devices are created once into the `CapabilityRegistry` singleton, the transport instances held by each device are effectively singleton in practice. This must be explicitly reviewed before `AddMigratedDeviceAdapters` supports hot-reconnect or multi-instance device scenarios.

---

## What the Original POP Architecture Had That Helix Does Not (Yet)

| POP feature | Helix status | Notes |
|---|---|---|
| `Info` state machine with fault propagation | Replaced by `DeviceResult` — no propagation yet | Helix capability result carries `DeviceResult` but no upstream fault routing |
| `Trend<T>` with X-axis and reaction time | `DeviceMeasurement` has fields but drivers don't populate them yet | Needs trend routing wired to `ITrendProvider` |
| Polling/scheduling per device | Not implemented | POP used background timers per device |
| Multi-device sequencing | Handled by Helix workflow engine | This is an improvement over POP |
| Hardware event callbacks | Not implemented | Will require `IOpcTransport` subscription API |
| Device hot-swap / reconnect | Not implemented | Port/endpoint is fixed at construction |

---

## Files Added by This Migration

### `Helix.Orchestration.Devices`

```
Transport/
  ISerialTransport.cs
  IModbusTransport.cs
  ITcpTransport.cs
  IOpcTransport.cs
Model/
  DeviceResult.cs
  DeviceMeasurement.cs
  DeviceKind.cs
  DeviceDetails.cs
Interfaces/
  IDevice.cs
  ISetPointDevice.cs
  IVapourtec.cs
  IHplc.cs
  IReactionPlatform.cs
BaseClasses/
  DeviceBase.cs
  SerialDeviceBase.cs
  TcpDeviceBase.cs
  OpcDeviceBase.cs
  ModbusDeviceBase.cs
  SetPointDeviceBase.cs
  StirrerDeviceBase.cs
  VacPumpDeviceBase.cs
Drivers/
  VapourtecR4.cs       ← scaffold, parity incomplete
  KnauerAzura.cs       ← scaffold, parity incomplete
  SystagOpcUaClient.cs ← scaffold, parity incomplete
```

### `Helix.Orchestration.Agent`

```
Transport/
  SerialTransport.cs
  ModbusTransport.cs
  TcpTransport.cs
  OpcTransport.cs
Adapters/Devices/
  DeviceOperationParameters.cs
  DeviceCapabilityAdapter.cs
  DeviceAdapterExtensions.cs
```

---

## Recommended Next Steps

1. **Complete `VapourtecR4` parity** — implement `GF`/`GT` live read-back, correct `SelectReagent`/`SelectSolvent` valve encoding, add `GetStatus()`, fault detection, and pump configuration. Extend `ISerialTransport` to accept `baudRate` in `Open()`.

2. **Complete `KnauerAzura` parity** — source the correct Knauer Azura command vocabulary from the SDK, add response validation, and add gradient/UV configuration.

3. **Complete `SystagOpcUaClient` parity** — add setpoint write methods, enforce correct OPC node path conventions, and extend `IOpcTransport` to support subscriptions/change notifications.

4. **Extend `DeviceCapabilityAdapter` or add typed adapters** — expose device-specific operations (flow rate, temperature, priming) as named capability operations.

5. **Wire `DeviceMeasurement` to `ITrendProvider`** — measurements returned by drivers should feed the existing Helix trend routing.

6. **Address the transport lifetime issue** — evaluate whether transports should be `Singleton` explicitly or whether a factory/reconnect pattern is needed.

7. **Add unit tests** — each driver should have tests using a mock `ISerialTransport` / `ITcpTransport` / `IOpcTransport` to verify command strings, response parsing, and error paths without hardware.

# Driver Inventory

Audit of device drivers migrated from `POP.DeviceCore` into the Helix platform.

Last updated: 2026-05-26 (Phase 2 Wave 1)

---

## Driver Table

| Driver name | Protocol | Transport interface | Connection type | Full POP vocabulary (from source) | Phase 2 implemented | POP.DeviceCore source file | Phase 2 status |
|---|---|---|---|---|---|---|---|
| VapourtecR4 | Serial | ISerialTransport | RS-232 @ 19200 baud | PN, PF, FW, KP, FR, PL, HT, HF, HP, GA, ST, GV, SV | PN, PF, FW, KP, FR, HT, HF, ST, GV | `POP.DeviceCore/Drivers/VapourtecR4.cs` | Phase 2 (this plan) |
| KnauerAzura | TCP | ITcpTransport | TCP | ACK/NAK command protocol, gradient config, UV config, run state polling | N/A | `POP.DeviceCore/Drivers/KnauerAzura.cs` | Phase 3 |
| SystagOpcUaClient | OPC UA | IOpcTransport → OpcDeviceTransport | OPC UA endpoint | Setpoint writes via OPC UA node IDs, change subscriptions for live updates | N/A | `POP.DeviceCore/Drivers/SystagOpcUaClient.cs` | Phase 3 |
| ModbusDeviceBase subclasses | Modbus | IModbusTransport | RS-485 | Register read/write | N/A | `POP.DeviceCore/Comm/` | Phase 3 (audit pending) |
| SetPointDeviceBase subclasses (StirrerDeviceBase, VacPumpDeviceBase) | Serial or TCP | — | Start/Stop/SetPoint/Limits | Start/Stop/SetPoint/Limits | N/A | documented in DEVICE-MIGRATION-HANDOVER.md | Phase 3 |

---

## VapourtecR4 POP Vocabulary Detail

| Command | Description | Phase 2 status |
|---|---|---|
| PN | Pump run (normally) | Implemented |
| PF | Pump flush | Implemented |
| FW | Firmware version query | Implemented |
| KP | Keep-alive / ping | Implemented |
| FR | Flow rate set | Implemented |
| PL | Pump limits query | Gap — not in Phase 2 |
| HT | Heater temperature set | Implemented |
| HF | Heater off | Implemented |
| HP | Heater power query | Gap — not in Phase 2 |
| GA | Get all state | Gap — not in Phase 2 |
| ST | Status query | Implemented |
| GV | Get value / measurement readback | Implemented |
| SV | Set value | Gap — not in Phase 2 |

---

## Parity Gaps

The following capabilities from the original `POP.DeviceCore` driver are **not** implemented in Phase 2 and are deferred to Phase 3:

1. **SelectReagent / SelectSolvent valve encoding** — The VapourtecR4 driver used numeric valve position encoding to select reagent and solvent lines. No Helix capability exposes this yet. The encoding scheme needs to be re-validated against hardware documentation before porting.

2. **Live measurement read-back via GF / GT** — `GF` (get flow) and `GT` (get temperature) are polled read-back commands that return live sensor values. The current Phase 2 `GV` implementation covers a subset; full GF/GT streaming via `StreamTrendsAsync` is deferred.

3. **Pump configuration (PL, HP)** — Querying pump limits (`PL`) and heater power (`HP`) is not implemented. These are needed for safety interlock logic and are deferred to a subsequent plan.

4. **Fault callbacks** — POP.DeviceCore exposed device fault events over an event/delegate model. The Helix architecture uses `IHelixAdapter.HealthCheckAsync` and `TransportWatchdog` for fault detection; however a structured fault-notification capability (e.g. a named capability that returns fault codes) has not been defined yet and is deferred.

---

## New Architecture

### DeviceAdapterBase Composition Model

The original `POP.DeviceCore` drivers used a **deep inheritance hierarchy** with base classes such as:

- `SetPointDeviceBase`
- `StirrerDeviceBase`
- `VacPumpDeviceBase`
- `ModbusDeviceBase`

These base classes tightly coupled protocol logic to transport implementations. They have been **deleted** in the Helix migration.

The replacement is a **composition model** centred on `DeviceAdapterBase` in `Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs`:

```
DeviceAdapterBase
├── Holds IDeviceTransport (injected, not inherited)
├── Holds ILogger (injected)
├── Dictionary<string, CapabilityHandler> built once in constructor
└── abstract RegisterCapabilities(CapabilityDispatchBuilder) — driver fills this
```

Key properties of the new model:

| Aspect | Old (POP) | New (Helix) |
|---|---|---|
| Driver reuse | Inheritance from protocol base | Composition via IDeviceTransport |
| Transport binding | Hardcoded in base constructor | Injected — any IDeviceTransport impl |
| Capability surface | Direct method calls | Named capabilities dispatched via CapabilityRegistry |
| Fault handling | Delegate events | TransportWatchdog + HealthCheckAsync |
| DI lifetime | Not applicable (desktop app) | Singleton — transport and adapter share lifetime |
| Testing | Required hardware or partial mocks | Full unit tests via IDeviceTransport mock |

**Transport registration helpers** (`AddHelixSerialAdapter`, `AddHelixTcpAdapter`, etc. in `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs`) wire up each adapter as a Singleton, paired with its transport and a `TransportWatchdog` for hot-reconnect without DI re-creation.

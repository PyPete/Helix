---
phase: 3
plan: phase-3-complete
subsystem: devices
tags: [knauer-azura, systag-opc-ua, tcp-driver, opc-driver, unit-tests, capability-registry]
---

# Phase 3: Remaining Device Drivers — Complete

## What was done

### T1 — KnauerAzura.cs (Wave 1, commit 71c5c58)
Created `Helix.Orchestration.Devices/Drivers/KnauerAzura.cs`:
- Pump-only scope (DAD + Thermostat deferred — Helix single-transport-per-adapter constraint)
- 6 capabilities: RemoteControl, Start, Stop, SetFlowRate, GetPumpStatus, GetIdentity
- Commands terminated with `\r` (0x0D); RAMP 15-field format: `RAMP:{flowUlMin},0,{percA},{percB},{percC},{percD},0,0,0,0,0,0,0,0,1\r`
- SetFlowRate validates sum of percentages ≤ 100; converts ml/min → µl/min
- GetPumpStatus parses STATUS response: state (10-value enum), flow (µl/min / 1000), pressure (kPa / 1000); falls back to `{ raw }` on parse failure
- StreamTrendsAsync polls STATUS every 5s; emits `knauer.pump.flowrate`, `knauer.pump.pressure`, `knauer.pump.state`
- Internal test constructor: `internal KnauerAzura(IDeviceTransport, ILogger<KnauerAzura>)`

### T2 — SystagOpcUaClient.cs + OpcTransportOptions fix (Wave 1, commit b42ce6d)
Fixed `Helix.Orchestration.Devices/Transports/Opc/OpcTransportOptions.cs`:
- Added `AutoAcceptUntrustedCertificates { get; init; } = false` (default secure)

Fixed `Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs`:
- Replaced hardcoded `AutoAcceptUntrustedCertificates = true` with `_options.AutoAcceptUntrustedCertificates`

Created `Helix.Orchestration.Devices/Drivers/SystagOpcUaClient.cs`:
- Dynamic browse-and-subscribe architecture — no hardcoded node IDs (matches real POP source)
- 4 capabilities: GetSystemInfo (root node read), ReadNode, WriteNode, SubscribeNode (all caller-supplied node IDs)
- StreamTrendsAsync: subscribes to `OpcDeviceTransport.DataReceived` IObservable<byte[]> via Channel<TrendSample>; emits `systag.{nodeId}` trend samples
- Internal test constructor: `internal SystagOpcUaClient(IDeviceTransport, ILogger<SystagOpcUaClient>)` (added in T3)

### T3 — Tests (Wave 2, commit 48185a0)
- `tests/Helix.Orchestration.Devices.Tests/Drivers/KnauerAzuraTests.cs` — 8 tests covering all 6 capabilities, RAMP field format, STATUS parsing, percent validation
- `tests/Helix.Orchestration.Devices.Tests/Drivers/SystagOpcUaClientTests.cs` — 5 tests covering ReadNode, WriteNode (pass-through, no range check), SubscribeNode, OpcTransportOptions default
- VapourtecR4Tests: 17/17 pass — no regression

### T4 — DI Registration (Wave 3, commit 1fd4baf)
Extended `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`:
- KnauerAzura: factory lambda for `TcpDeviceTransport` (192.168.1.101:10001, Delimiter framing, 0x0D byte) + `KnauerAzura` singleton
- SystagOpcUaClient: factory lambda for `OpcDeviceTransport` (opc.tcp://localhost:4840, AutoAcceptUntrustedCertificates=false) + `SystagOpcUaClient` singleton
- CapabilityRegistry lambda extended: registers all 6 KnauerAzura capabilities + all 4 SystagOpcUaClient capabilities via HelixAdapterCapabilityBridge

### T5 — Documentation (Wave 4, commit 2b02793)
- `Helix.Orchestration/docs/driver-inventory.md` — KnauerAzura and SystagOpcUaClient Phase 3 column updated to ✓
- `.planning/REQUIREMENTS.md` — B3, B4, B5 marked Complete

## Test results

- `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "KnauerAzura"` — Passed: 8, Failed: 0
- `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "SystagOpcUaClient"` — Passed: 5, Failed: 0
- `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "VapourtecR4"` — Passed: 17, Failed: 0 (no regression)
- `dotnet build Helix.Orchestration.slnx -c Debug` — 0 errors, 16 warnings (all pre-existing)

## Deviations

- T3 added `internal SystagOpcUaClient(IDeviceTransport, ILogger<SystagOpcUaClient>)` test constructor to `SystagOpcUaClient.cs` — required for NSubstitute injection pattern; mirrors KnauerAzura and VapourtecR4.

## Build status

`dotnet build Helix.Orchestration.slnx -c Debug` — **0 errors, 16 warnings** (pre-existing: MSB3277 System.Reactive version conflict, CS8425 on DeviceAdapterBase.StreamTrendsAsync, xUnit fixture warning).

## Commits

- `71c5c58` — T1: KnauerAzura.cs
- `b42ce6d` — T2: SystagOpcUaClient.cs + OpcTransportOptions fix
- `48185a0` — T3: KnauerAzuraTests + SystagOpcUaClientTests
- `1fd4baf` — T4: CapabilityRegistry DI registration
- `2b02793` — T5: docs B3/B4/B5 complete + driver-inventory

## Key files

Created:
- `Helix.Orchestration.Devices/Drivers/KnauerAzura.cs`
- `Helix.Orchestration.Devices/Drivers/SystagOpcUaClient.cs`
- `tests/Helix.Orchestration.Devices.Tests/Drivers/KnauerAzuraTests.cs`
- `tests/Helix.Orchestration.Devices.Tests/Drivers/SystagOpcUaClientTests.cs`

Modified:
- `Helix.Orchestration.Devices/Transports/Opc/OpcTransportOptions.cs` — added AutoAcceptUntrustedCertificates
- `Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs` — wired option (removed hardcoded true)
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` — DI + CapabilityRegistry registrations
- `Helix.Orchestration/docs/driver-inventory.md` — Phase 3 ✓
- `.planning/REQUIREMENTS.md` — B3/B4/B5 Complete

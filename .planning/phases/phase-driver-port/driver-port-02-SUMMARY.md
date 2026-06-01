---
phase: driver-port
plan: "02"
subsystem: Helix.Orchestration.Devices
tags: [driver-port, tcp, opc-ua, mettler, mass-spec]
dependency_graph:
  requires: []
  provides:
    - Microsaic TCP driver
    - EasyMax OPC-UA driver (iControl)
    - Fbrm OPC-UA driver (iCFBRM)
    - ReactIr OPC-UA driver (iCIR)
    - IMettlerOpcCommand interface
  affects:
    - Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs
tech_stack:
  added:
    - IMettlerOpcCommand interface (local, from POP.Shared.DeviceInterfaces contract)
    - OpcConnectionEnum (local enum, POP-specific)
    - OpcDataChangeReceivedEventHandler (local delegate, Helix uses OPCFoundation not Opc.UaFx)
  patterns:
    - TCP driver with dual constructors (public TcpDeviceTransport + internal IDeviceTransport)
    - OPC-UA driver facade pattern (IMettlerOpcCommand _opc field, Transport stub)
    - NSubstitute mock for both IDeviceTransport and IMettlerOpcCommand in tests
key_files:
  created:
    - Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs
    - Helix.Orchestration.Devices/Drivers/Microsaic.cs
    - Helix.Orchestration.Devices/Drivers/EasyMax.cs
    - Helix.Orchestration.Devices/Drivers/Fbrm.cs
    - Helix.Orchestration.Devices/Drivers/ReactIr.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/MicrosaicTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/EasyMaxTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/FbrmTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/ReactIrTests.cs
  modified:
    - Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs
decisions:
  - OpcDataChangeReceivedEventHandler declared locally because Helix uses OPCFoundation.NetStandard.Opc.Ua.Client, not Opc.UaFx — delegate signatures are compatible
  - OpcConnectionEnum declared locally (POP-specific, not in OPCFoundation package)
  - Microsaic command terminator confirmed as "\n\n" (CmdEnd field in POP source)
  - EasyMax StreamTrendsAsync polls _opc.GetTrendsAtNode every 2s (POP GetTrends pattern)
  - Fbrm and ReactIr do NOT override StreamTrendsAsync (has_trends=NO per RESEARCH.md)
  - IMettlerOpcCommand production registration throws NotImplementedException (placeholder until real OPC client adapter is available)
metrics:
  duration: "~45 minutes"
  completed: "2026-06-01"
  tasks_completed: 4
  files_created: 9
  files_modified: 1
  tests_added: 44
---

# Phase driver-port Plan 02: TCP + OPC-UA Drivers (4 drivers) Summary

**One-liner:** Microsaic TCP driver with 9 capabilities + EasyMax/Fbrm/ReactIr OPC-UA drivers via IMettlerOpcCommand facade, all registered in DeviceAdapterExtensions with device-prefixed capabilities.

## Tasks Completed

| Wave | Task | Description | Commit |
|------|------|-------------|--------|
| 0 | 0.1 | Declare IMettlerOpcCommand interface | ec3fe52 |
| 1 | 1.1 | Port Microsaic (TCP) + tests | c2b734e |
| 2 | 2.1 | Port EasyMax (OPC-UA) + tests | 2307d05 |
| 2 | 2.2 | Port Fbrm (OPC-UA) + tests | 2307d05 |
| 2 | 2.3 | Port ReactIr (OPC-UA) + tests | 2307d05 |
| 3 | 3.1 | DI registration for all 4 | edafa13 |

## Drivers Ported

### Microsaic (TCP)

- **Transport:** `TcpDeviceTransport`, `\n\n` terminator (CmdEnd from POP source)
- **Capabilities:** GetAllStatusDetails, SetAttenuation, StartMra, StopMra, SetMakeUpFlowRate, StartMakeUpFlow, StopMakeUpFlow, InitialiseFullScanParameters, StopDataAcquisition
- **StreamTrendsAsync:** Polls `GET LIVE_DATA\n\n` every 250ms, yields `microsaic.spectrum.raw` as JSON-serialized intensity array
- **Tests:** 19 NSubstitute tests, command strings verified to match POP source exactly

### EasyMax (OPC-UA, iControl)

- **Pattern:** Facade — `IMettlerOpcCommand _opc`, Transport stub, no `Transport.SendReceiveAsync` calls
- **Capabilities:** FindActiveSystems, CreateSubscription, IsDevice, GetTrendValues
- **StreamTrendsAsync:** Polls `_opc.GetTrendsAtNode("ns=2;s=Local.iControl.{system}.Trends")` every 2s
- **OPC node IDs verified:** `ns=2;s=Local.iControl`, `ns=2;Local.iControl.ConnectionStatus`, `ns=2;Local.iControl.{exp}.ExperimentName`, `ns=2;Local.iControl.{exp}.Trends`
- **Tests:** 9 NSubstitute tests

### Fbrm (OPC-UA, iCFBRM)

- **Pattern:** Same facade as EasyMax
- **Capabilities:** FindActiveSystems, CreateSubscription, IsDevice
- **StreamTrendsAsync:** NOT overridden (has_trends=NO)
- **OPC node IDs verified:** `ns=2;s=Local.iCFBRM`, `ns=2;Local.iCFBRM.ConnectionStatus`, `ns=2;Local.iCFBRM.{probe}.ExperimentName`
- **Tests:** 6 NSubstitute tests including `FbrmDoesNotOverrideStreamTrendsAsync` guard

### ReactIr (OPC-UA, iCIR)

- **Pattern:** Same facade as EasyMax
- **Capabilities:** FindActiveSystems, CreateSubscription, IsDevice, UpdateSamplingInterval, GetSamplingRate, Pause, Resume
- **StreamTrendsAsync:** NOT overridden (has_trends=NO)
- **OPC node IDs verified:** `ns=2;s=Local.iCIR`, `ns=2;Local.iCIR.ConnectionStatus`, `ns=2;Local.iCIR.{probe}.Methods`, `ns=2;Local.iCIR.{probe}.Methods.SetSamplingInterval`, `ns=2;Local.iCIR.{probe}.ProbeStatus`, `ns=2;Local.iCIR.{probe}.CurrentSamplingInterval`
- **Tests:** 10 NSubstitute tests including `ReactIrDoesNotOverrideStreamTrendsAsync` guard

## Test Results

```
Passed!  - Failed: 0, Passed: 44, Skipped: 0, Total: 44
```

- MicrosaicTests: 19 tests
- EasyMaxTests: 9 tests
- FbrmTests: 6 tests
- ReactIrTests: 10 tests

## Deviations from Plan

### Auto-adapted during implementation

**1. [Rule 1 - Deviation] OpcDataChangeReceivedEventHandler declared locally**
- **Found during:** Task 0.1
- **Issue:** POP's `IMettlerOpcCommand.SubscribeToNode` uses `OpcDataChangeReceivedEventHandler` from `Opc.UaFx.Client`. Helix uses `OPCFoundation.NetStandard.Opc.Ua.Client` (namespace `Opc.Ua`) — `Opc.UaFx` is not referenced.
- **Fix:** Declared `OpcDataChangeReceivedEventHandler` as a local delegate `(object sender, object? e)` in `IMettlerOpcCommand.cs`. The delegate signature is compatible for test purposes (POP tests call with `OpcMonitoredItem` sender — which is typed as `object` here).
- **Files modified:** `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs`

**2. [Rule 2 - Missing] `GetAllStatusDetails` uses polling retry loop**
- **Found during:** Task 1.1
- **Issue:** POP source uses `while (stringOk && s.Elapsed < 5s)` retry loops because the Microsaic device may return empty responses. The original plan described single `Transport.SendReceiveAsync` calls.
- **Fix:** Implemented `WaitForResponseAsync` and `WaitForMakeupPumpAsync` helpers that poll until content is present or timeout. This is faithful to POP source behavior. In tests, the single mock response is returned immediately (no timeout loop needed for tests).

**3. [Rule 1 - Deviation] Wave 2 tasks committed together (single commit)**
- **Found during:** Task 2.1-2.3
- **Issue:** Plan says tasks 2.1/2.2/2.3 can be parallelized. I implemented them sequentially but committed all three in one commit (2307d05) which is functionally equivalent.

## Known Stubs

| Stub | File | Reason |
|------|------|--------|
| `IMettlerOpcCommand` singleton throws `NotImplementedException` | `DeviceAdapterExtensions.cs` | Production OPC client adapter for Mettler devices not yet implemented. Replace before starting application. |
| TCP host `"10.0.0.1"` for Microsaic | `DeviceAdapterExtensions.cs` | Placeholder hostname from POP source comments. Override via appsettings in production. |
| OPC endpoints `opc.tcp://localhost:4841/4842/4843` | `DeviceAdapterExtensions.cs` | Placeholder endpoints. Override via appsettings in production. |

## Threat Flags

None. No new network endpoints, auth paths, or schema changes were introduced beyond what the plan specifies.

## Self-Check: PASSED

- `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs` exists
- `Helix.Orchestration.Devices/Drivers/Microsaic.cs` exists with dual constructors
- `Helix.Orchestration.Devices/Drivers/EasyMax.cs`, `Fbrm.cs`, `ReactIr.cs` exist using `_opc` field
- All 4 drivers appear in `DeviceAdapterExtensions.AddPlan02NetworkAdapters()`
- Capability prefixes `microsaic.`, `easymax.`, `fbrm.`, `reactir.` in CapabilityRegistry builder
- `dotnet build Helix.Orchestration.slnx` passes (0 errors)
- 44 tests pass (0 failures)

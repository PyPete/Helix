---
phase: 2
plan: phase-2-complete
subsystem: devices
tags: [vapourtec-r4, driver, di-lifetime, trend-aggregator, unit-tests]
---

# Phase 2: VapourtecR4 Reference Driver — Complete

## What was done

### T1 — Driver Audit Document (Wave 1, commit 9e6817a)
Created `Helix.Orchestration/docs/driver-inventory.md` with driver table (VapourtecR4, KnauerAzura, SystagOpcUaClient, ModbusDeviceBase, SetPointDeviceBase), separate "Full POP vocabulary" vs "Phase 2 implemented" columns, Parity Gaps section, and New Architecture section.

### T2 — Verify ISerialTransport.Open / Remove Dead DI Registrations (Wave 1, commit 9e6817a)
Confirmed `void Open(string portName, int baudRate = 9600)` already present. Removed three dead `AddTransient` registrations (ISerialTransport, IModbusTransport, ITcpTransport) from `DeviceAdapterExtensions.cs`.

### T3 — Fix DI Lifetime: Adapters and Transports as Singleton (Wave 1, commit 9e6817a)
Changed `AddScoped` to `AddSingleton` in all five `AddHelix*Adapter` methods in `DevicesServiceCollectionExtensions.cs`. Added DI Lifetime Note (B7) comment block.

### T4 — Implement VapourtecR4.cs (Wave 2, commit 1bcf1f2)
Created `Helix.Orchestration.Devices/Drivers/VapourtecR4.cs`:
- 10 capabilities: Start, Stop, SetFlowRate (pumps 0-3 with FW routing, rate clamped 0-9.99), SetTemperature, SelectReagent/Solvent/LoopFill/LoopInject (KP valve maps), GetFirmwareVersion (GV), GetTemperatures (HT parsing)
- ERR-prefix detection in all capabilities returns Failed result
- StreamTrendsAsync override: polls HT + HF every 2s, emits vapourtec.temperature.{A-D} and vapourtec.flow.{pumpA/pumpB}

### T5 — Fix TrendAggregator Scalar Routing (Wave 2, commit e53b683)
- Added `_scalarLatest` ConcurrentDictionary to `InMemoryTrendProvider`
- Added `PushScalar<T>` and `TryGetScalar` public methods
- Updated `IsTrendAvailableAsync` and `GetAllTrendNames` to include scalar store
- Added `ScalarValue<double>`, `ScalarValue<float>`, `ScalarValue<int>` cases to `TrendAggregator.ProcessAsync`
- Changed `default` branch log level from LogDebug to LogWarning

### T6 — Wire VapourtecR4 into CapabilityRegistry via Bridge (Wave 3, commit 5b9212f)
- Created `HelixAdapterCapabilityBridge`: translates `IHelixAdapter` (Devices) to Core's `ICapabilityInvoker`
- Replaced Phase 1 placeholder CapabilityRegistry lambda with VapourtecR4-backed registration
- Registered `SerialTransportOptions` (COM3/19200 placeholder), `SerialDeviceTransport`, `VapourtecR4` as Singletons
- Bridge iterates `adapter.GetCapabilities()` and registers each via `registry.Register()`

### T7 — Tests (Wave 4, commit 2edd5ab)
- `tests/Helix.Orchestration.Devices.Tests/Drivers/VapourtecR4Tests.cs` — 17 tests covering all 10 capabilities, ERR handling, invalid pump index, StreamTrendsAsync emission
- `tests/Helix.Orchestration.Agent.Tests/Streaming/TrendAggregatorScalarTests.cs` — 5 tests covering ScalarValue<double/float/int> routing, unsupported type handling, IsTrendAvailableAsync

## Test results

- `dotnet test tests/Helix.Orchestration.Devices.Tests/` — Passed: 17, Failed: 0
- `dotnet test tests/Helix.Orchestration.Agent.Tests/` — Passed: 5 (scalar filter), Failed: 0
- `dotnet build Helix.Orchestration.slnx` — 0 errors, 24 pre-existing warnings

## Deviations

- Wave 1 VapourtecR4.cs was already present in a prior session commit (1bcf1f2) — no re-implementation needed; T4 wording adjusted to confirm.
- T6 had also been committed (5b9212f) from a prior session; Wave 3 confirmed the work was done.
- Only T7 test files were net-new work for this session.

## Build status

`dotnet build Helix.Orchestration.slnx -c Debug` — **0 errors, 24 warnings** (pre-existing: MSB3277 System.Reactive version conflict, CS8425 on DeviceAdapterBase.StreamTrendsAsync).

## Commits

- `9e6817a` — Wave 1: T1+T2+T3
- `1bcf1f2` — T4: VapourtecR4.cs
- `e53b683` — T5: TrendAggregator scalar routing
- `5b9212f` — T6: CapabilityRegistry bridge wiring
- `2edd5ab` — T7: VapourtecR4Tests + TrendAggregatorScalarTests

## Key files

Created:
- `Helix.Orchestration/docs/driver-inventory.md`
- `Helix.Orchestration.Devices/Drivers/VapourtecR4.cs`
- `Helix.Orchestration.Agent/Adapters/Devices/HelixAdapterCapabilityBridge.cs`
- `tests/Helix.Orchestration.Devices.Tests/Drivers/VapourtecR4Tests.cs`
- `tests/Helix.Orchestration.Agent.Tests/Streaming/TrendAggregatorScalarTests.cs`

Modified:
- `Helix.Orchestration.Devices/Transport/ISerialTransport.cs`
- `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs`
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`
- `Helix.Orchestration.Agent/Streaming/TrendAggregator.cs`
- `Helix.Orchestration.Agent/Trends/InMemoryTrendProvider.cs`
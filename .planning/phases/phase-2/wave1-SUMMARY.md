---
phase: 2
plan: wave1
subsystem: devices
tags: [driver-inventory, di-lifetime, transport, vapourtec]
---

# Phase 2 Wave 1: Driver Audit, Dead Transport DI Removal, Adapter Singleton Lifetimes

## What was done

### T1 — Driver Audit Document
Created `Helix.Orchestration/docs/driver-inventory.md` (new directory `Helix.Orchestration/docs/` created).

The document contains:
- A driver table for VapourtecR4, KnauerAzura, SystagOpcUaClient, ModbusDeviceBase subclasses, and SetPointDeviceBase subclasses with protocol, transport interface, connection type, full POP vocabulary, Phase 2 implementation status, source file, and phase status.
- VapourtecR4 command-level breakdown (PN/PF/FW/KP/FR/PL/HT/HF/HP/GA/ST/GV/SV) with per-command Phase 2 status.
- Parity Gaps section: SelectReagent/SelectSolvent valve encoding, live measurement read-back (GF/GT), pump configuration (PL/HP), fault callbacks.
- New Architecture section describing how DeviceAdapterBase composition model replaces deleted inheritance base classes.

### T2 — ISerialTransport.Open Signature Verification and Dead DI Removal
- Confirmed `void Open(string portName, int baudRate = 9600)` was already present in `ISerialTransport.cs`.
- Added inline comment `// baudRate parameter added; see B6.` to the Open method signature.
- Removed three dead `AddTransient` registrations from `DeviceAdapterExtensions.AddMigratedDeviceAdapters()`:
  - `services.AddTransient<ISerialTransport, SerialTransport>()`
  - `services.AddTransient<IModbusTransport, ModbusTransport>()`
  - `services.AddTransient<ITcpTransport, TcpTransport>()`
- Dropped the now-unused `using Helix.Orchestration.Agent.Transport` and `using Helix.Orchestration.Devices.Transport` directives.
- The concrete transport classes in `Helix.Orchestration.Agent/Transport/` were not deleted — only their DI registrations.
- CapabilityRegistry singleton registration was preserved.

### T3 — Fix DI Lifetime: Adapters and Transports as Singleton
- Changed `AddScoped` to `AddSingleton` in all five `AddHelix*Adapter` methods in `DevicesServiceCollectionExtensions.cs`:
  - `AddHelixSerialAdapter` — IDeviceTransport/TAdapter/IHelixAdapter/TransportWatchdog
  - `AddHelixTcpAdapter` — same
  - `AddHelixModbusAdapter` — same
  - `AddHelixOpcAdapter` — same
  - `AddHelixCDllAdapter` — same
- Added DI Lifetime Note (B7) comment block at top of file explaining the captive dependency constraint.

## Deviations

- `Helix.Orchestration/` directory did not exist at the repo root. Created it as a docs-only container since no `.csproj` was specified in the task and the existing project directories use the `Helix.Orchestration.*` naming pattern. The driver inventory is a planning/reference artefact, not compiled code.

## Build status

`dotnet build Helix.Orchestration.slnx -c Debug` — **0 errors, 24 warnings** (pre-existing warnings: System.Reactive version conflict MSB3277, CS8425 on DeviceAdapterBase.StreamTrendsAsync, xUnit1041 on PostgresRepositoriesTests). All warnings were present before Wave 1 changes.

## Commit

`9e6817a` — feat(phase-2): Wave 1 — driver audit doc, remove dead transport DI, fix adapter lifetimes to Singleton

## Key files

Created:
- `Helix.Orchestration/docs/driver-inventory.md`

Modified:
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`
- `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs`
- `Helix.Orchestration.Devices/Transport/ISerialTransport.cs`

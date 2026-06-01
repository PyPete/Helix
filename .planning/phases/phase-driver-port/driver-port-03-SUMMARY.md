---
phase: driver-port
plan: "03"
subsystem: Helix.Orchestration.Devices
tags: [drivers, facade-pattern, file-io, sql, io-link, cdll, pinvoke, di-registration]
dependency_graph:
  requires: [driver-port-01, driver-port-02]
  provides: [AtlasReactionSystem, Crystalline, IfmMasterController, OceanViewDriver, OceanViewUv, OceanViewNir, PicoTc08, PicoTc08LowLevelWrapper]
  affects: [DeviceAdapterExtensions, CapabilityRegistry]
tech_stack:
  added:
    - NullDeviceTransport (stub IDeviceTransport for facade drivers)
    - ThermoCoupleType / PicoTempUnit enums (local copies of POP types)
    - 9 domain interfaces in Helix.Orchestration.Devices.Interfaces
  patterns:
    - Facade pattern: DeviceAdapterBase subclass + domain interface + NullDeviceTransport stub
    - Plain class P/Invoke wrappers: OceanViewDriver, PicoTc08LowLevelWrapper
key_files:
  created:
    - Helix.Orchestration.Devices/Interfaces/IOceanViewDriver.cs
    - Helix.Orchestration.Devices/Interfaces/IPicoApi.cs
    - Helix.Orchestration.Devices/Interfaces/ICrystallineSqlClient.cs
    - Helix.Orchestration.Devices/Interfaces/IPopIoLinkClient.cs
    - Helix.Orchestration.Devices/Interfaces/IDirectoryInfo.cs
    - Helix.Orchestration.Devices/Interfaces/IFileInfo.cs
    - Helix.Orchestration.Devices/Interfaces/IReadCsvFile.cs
    - Helix.Orchestration.Devices/Interfaces/IGetProcess.cs
    - Helix.Orchestration.Devices/Interfaces/IFile.cs
    - Helix.Orchestration.Devices/Model/ThermoCoupleType.cs
    - Helix.Orchestration.Devices/Model/PicoTempUnit.cs
    - Helix.Orchestration.Devices/Drivers/AtlasReactionSystem.cs
    - Helix.Orchestration.Devices/Drivers/Crystalline.cs
    - Helix.Orchestration.Devices/Drivers/OceanViewDriver.cs
    - Helix.Orchestration.Devices/Drivers/OceanViewUv.cs
    - Helix.Orchestration.Devices/Drivers/OceanViewNir.cs
    - Helix.Orchestration.Devices/Drivers/PicoTc08LowLevelWrapper.cs
    - Helix.Orchestration.Devices/Drivers/PicoTc08.cs
    - Helix.Orchestration.Devices/Drivers/IfmMasterController.cs
    - Helix.Orchestration.Devices/Transports/Null/NullDeviceTransport.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/AtlasReactionSystemTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/CrystallineTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/OceanViewUvTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/OceanViewNirTests.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/PicoTc08Tests.cs
    - tests/Helix.Orchestration.Devices.Tests/Drivers/IfmMasterControllerTests.cs
  modified:
    - Helix.Orchestration.Devices/Drivers/Microsaic.cs (bug fix: yield-in-catch CS1626)
    - Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs (added AddPlan03SpecialAdapters)
decisions:
  - "POP Info type mapped to Helix DeviceResult in all 9 domain interfaces — POP types do not exist in Helix"
  - "ThermoCoupleType and PicoTempUnit declared as Helix-local enums in Model/ — required by IPicoApi"
  - "OceanViewDriver methods stubbed with NotImplementedException — NetOceanDirect vendor SDK not referenced in Helix"
  - "IFileInfo declared alongside IDirectoryInfo — required by AtlasReactionSystem GetLastUpdatedFileInDirectory"
  - "StreamTrendsAsync _currentRow initialised to -1 (not 3) — enables first-poll data emission"
  - "IfmMasterController uses System.Text.Json.Nodes.JsonNode for io_link.json parsing — Newtonsoft not in Helix"
  - "Blocking .Result in IfmMasterController.GetTrends replaced with await in StreamTrendsAsync"
metrics:
  duration: "~90 minutes"
  completed: "2026-06-01"
  tasks_completed: 8
  files_created: 27
  files_modified: 2
  tests_added: 63
  test_pass_rate: "262/262 (100%)"
---

# Phase driver-port Plan 03: Special/Custom Transport Drivers Summary

Facade pattern porting of 8 drivers using non-standard transports (File I/O, SQL Server, IO-Link HTTP, vendor C DLLs) into Helix as DeviceAdapterBase subclasses with domain interfaces.

## What Was Built

8 drivers ported across 4 waves:

**Wave 0 — Domain Interfaces (prerequisite)**
Declared 9 interface files + 2 enum files:
- `IOceanViewDriver`, `IPicoApi`, `ICrystallineSqlClient`, `IPopIoLinkClient`
- `IDirectoryInfo`, `IFileInfo`, `IReadCsvFile`, `IGetProcess`, `IFile`
- `ThermoCoupleType`, `PicoTempUnit` enums

**Wave 1 — File I/O and SQL Facade Drivers**
- `AtlasReactionSystem`: DeviceAdapterBase facade over `IDirectoryInfo`, `IReadCsvFile`, `IGetProcess`. Capabilities: Open, IsDevice, Close, GetLatestData. StreamTrendsAsync polls CSV log files at 2-second intervals.
- `Crystalline`: DeviceAdapterBase facade over `ICrystallineSqlClient`. Capabilities: CreateSession, CreateSubscription, Close, FindActiveSystems, IsDevice. StreamTrendsAsync polls scalar reactor data (image trends omitted).

**Wave 2 — CDll and IO-Link Drivers**
- `OceanViewDriver`: Plain class implementing `IOceanViewDriver`. Methods stubbed with NotImplementedException pending NetOceanDirect NuGet reference.
- `OceanViewUv` / `OceanViewNir`: DeviceAdapterBase facades over `IOceanViewDriver`. ~15 capabilities each (Open, IsDevice, GetSpectrum, GetWavelengths, electric/nonlinear correction, integration time, boxcar, scans-to-average). StreamTrendsAsync NOT overridden (spectrum is non-scalar Matrix).
- `PicoTc08LowLevelWrapper`: Plain class implementing `IPicoApi` via P/Invoke to usbtc08.dll.
- `PicoTc08`: DeviceAdapterBase facade over `IPicoApi`. Capabilities: FindActiveDevices, SetActiveDevice, IsDevice, Close, SetChannelThermocoupleType, SetChannelsThermocoupleType, GetThermocoupleInformation, GetUniqueId, GetDeviceInfo. StreamTrendsAsync polls all 8 channels every 2 seconds.
- `IfmMasterController`: DeviceAdapterBase facade over `IPopIoLinkClient` + `IFile`. Capabilities: FindActiveSystems, Open, Close, GetAllProcessData, IsDevice. StreamTrendsAsync uses `await` (replaced blocking `.Result`).

**Wave 3 — DI Registration**
- `NullDeviceTransport`: No-op `IDeviceTransport` implementation for all facade drivers.
- `AddPlan03SpecialAdapters()`: Registers all 8 drivers. OceanViewDriver and PicoTc08LowLevelWrapper registered as interface implementations only (not in CapabilityRegistry). Six DeviceAdapterBase drivers registered with prefixes: `atlasreactionsystem.`, `crystalline.`, `ifmmastercontroller.`, `oceanviewuv.`, `oceanviewnir.`, `picotc08.`.

## Test Results

- 63 new tests added across 6 test files
- 262 total tests passing (100%)
- All Plan 03 drivers confirmed to NOT call `Transport.SendReceiveAsync`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed CS1626 yield-in-catch in Microsaic.cs**
- **Found during:** Wave 0 build verification (pre-existing bug from Plan 02)
- **Issue:** `yield break` inside try/catch blocks at lines 281 and 291 of Microsaic.cs caused CS1626 compiler error preventing solution build
- **Fix:** Introduced `cancelled` and `dataFetchFailed` flags to move yield break outside the try/catch blocks
- **Files modified:** `Helix.Orchestration.Devices/Drivers/Microsaic.cs`
- **Commit:** d573fca

**2. [Rule 2 - Missing Critical] Declared IFileInfo interface**
- **Found during:** Wave 0 — AtlasReactionSystem POP source uses `IFileInfo` type returned by `IDirectoryInfo.GetFiles()`
- **Fix:** Added `IFileInfo.cs` to `Helix.Orchestration.Devices/Interfaces/`
- **Files modified:** `Helix.Orchestration.Devices/Interfaces/IFileInfo.cs`

**3. [Rule 2 - Type Mapping] POP `Info` → Helix `DeviceResult` in all interfaces**
- **Found during:** Wave 0 — POP `Info` type does not exist in Helix
- **Fix:** All 9 domain interfaces use `DeviceResult` instead of `Info` for return types; `ThermoCoupleType` and `PicoTempUnit` enums declared as Helix-local copies

**4. [Rule 2 - JSON Library] Newtonsoft.Json replaced with System.Text.Json.Nodes in IfmMasterController**
- **Found during:** Wave 2.3 — POP IfmMasterController uses `Newtonsoft.Json.Linq.JObject` which is not referenced in Helix
- **Fix:** Used `System.Text.Json.Nodes.JsonNode` for io_link.json parsing

**5. [Rule 1 - Logic] StreamTrendsAsync `_currentRow` initialised to -1 (not 3)**
- **Found during:** Wave 1.1 test execution — `StreamTrendsAsync_WithOpenPath_EmitsTrendSamples` failed
- **Issue:** Initial `_currentRow = 3` prevented first-poll data emission when CSV had 3 rows (rows 0-2)
- **Fix:** Changed to `_currentRow = -1` with condition `dt.Rows.Count - 1 > _currentRow`

## Known Stubs

- `OceanViewDriver` all methods: `throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced")` — NetOceanDirect NuGet package not referenced in Helix. All methods are compile-safe stubs until the package is added.
- `IDirectoryInfo`, `IReadCsvFile`, `IGetProcess`, `ICrystallineSqlClient`, `IPopIoLinkClient`, `IFile` DI registrations: `throw new NotImplementedException(...)` — production implementations not provided by Plan 03. Intentional per plan design: these require concrete implementations wired up for each deployment target.
- `PicoTc08LowLevelWrapper` P/Invoke calls: compilable but will throw `DllNotFoundException` at runtime without `usbtc08.dll` in native binaries.

## Threat Flags

None — no new network endpoints, auth paths, or trust boundary crossings introduced. DI placeholder `throw new NotImplementedException` registrations are fail-fast (no silent degradation).

## Self-Check: PASSED

- All 27 created files present in Helix project
- Commits d573fca, 2bf33a9, a0ff1ec, a44beb1 verified in git log
- `dotnet build Helix.Orchestration.slnx` passes (0 errors)
- `dotnet test tests/Helix.Orchestration.Devices.Tests/` — 262 passed, 0 failed

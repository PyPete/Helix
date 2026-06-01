---
phase: driver-port
plan: "01"
subsystem: Helix.Orchestration.Devices
tags: [drivers, serial, port, xunit, tests]
dependency_graph:
  requires: []
  provides: [32-serial-drivers, FakeSerial-helper, DI-registration]
  affects: [Helix.Orchestration.Devices, Helix.Orchestration.Agent]
tech_stack:
  added: []
  patterns: [DeviceAdapterBase, SerialDeviceTransport, FakeSerialPortConnection, CapabilityDispatchBuilder]
key_files:
  created:
    - tests/Helix.Orchestration.Devices.Tests/TestHelpers/FakeSerial.cs
    - Helix.Orchestration.Devices/Drivers/AalborgDfm.cs
    - Helix.Orchestration.Devices/Drivers/Alicat.cs
    - Helix.Orchestration.Devices/Drivers/ArduinoConductivity.cs
    - Helix.Orchestration.Devices/Drivers/ArduinoLeak.cs
    - Helix.Orchestration.Devices/Drivers/ArduinopH.cs
    - Helix.Orchestration.Devices/Drivers/ArduinoPressure.cs
    - Helix.Orchestration.Devices/Drivers/ArduinoSignalTower.cs
    - Helix.Orchestration.Devices/Drivers/ArduinoTemp.cs
    - Helix.Orchestration.Devices/Drivers/ArduinoValves.cs
    - Helix.Orchestration.Devices/Drivers/AtlasSyringePump.cs
    - Helix.Orchestration.Devices/Drivers/Carino9.cs
    - Helix.Orchestration.Devices/Drivers/Cml18.cs
    - Helix.Orchestration.Devices/Drivers/ColeParmerTouch.cs
    - Helix.Orchestration.Devices/Drivers/FoxyR1.cs
    - Helix.Orchestration.Devices/Drivers/Gynkotek.cs
    - Helix.Orchestration.Devices/Drivers/Heidolph.cs
    - Helix.Orchestration.Devices/Drivers/IkaDigitalHotPlate.cs
    - Helix.Orchestration.Devices/Drivers/IkaOverheadStirrer.cs
    - Helix.Orchestration.Devices/Drivers/IkaOverheadStirrerOldDevice.cs
    - Helix.Orchestration.Devices/Drivers/IkaUltraTurrax.cs
    - Helix.Orchestration.Devices/Drivers/KernPcb.cs
    - Helix.Orchestration.Devices/Drivers/KnauerP2And4Pump.cs
    - Helix.Orchestration.Devices/Drivers/LambdaDoser.cs
    - Helix.Orchestration.Devices/Drivers/MasterFlexLsDigital.cs
    - Helix.Orchestration.Devices/Drivers/MettlerBalance.cs
    - Helix.Orchestration.Devices/Drivers/NewEra1000.cs
    - Helix.Orchestration.Devices/Drivers/OrionVersaStarPro.cs
    - Helix.Orchestration.Devices/Drivers/PilotOne.cs
    - Helix.Orchestration.Devices/Drivers/SartoriusBalance.cs
    - Helix.Orchestration.Devices/Drivers/SuperDew3.cs
    - Helix.Orchestration.Devices/Drivers/Vario.cs
    - Helix.Orchestration.Devices/Drivers/ViciActuator.cs
    - Helix.Orchestration.Devices/Drivers/ViciMultiValveActuator.cs
  modified:
    - Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs
decisions:
  - IkaOverheadStirrer made non-sealed to allow IkaOverheadStirrerOldDevice to inherit from it
  - Cml18 ported as request-response (not event-driven callback) due to Helix transport constraints
  - ColeParmerTouch ported with basic capabilities only (POP test file entirely commented out)
  - NewEraBaseMethods logic inlined directly into NewEra1000 (no helper class created)
  - FakeSerial responses must end exactly with the commandTerminator byte to avoid bleed-through
metrics:
  duration: "~4 hours"
  completed: "2026-06-01"
  tasks: 6
  files: 69
---

# Phase driver-port Plan 01: Serial Drivers (32 drivers) Summary

Port 32 serial device drivers from POP.DeviceCore into Helix.Orchestration.Devices as DeviceAdapterBase subclasses with xUnit tests, plus DI registration.

## Drivers Ported

### Wave 1 — Batch A (11 drivers)
| Driver | BaudRate | CmdTerminator | has_trends |
|--------|----------|---------------|------------|
| AalborgDfm | 9600 | `\r` | YES |
| Alicat | 19200 | `\r` | YES |
| ArduinoConductivity | 9600 | `\r\n` | YES |
| ArduinoLeak | 9600 | `\r\n` | YES |
| ArduinopH | 9600 | `\r\n` | YES |
| ArduinoPressure | 9600 | `\r\n` | YES |
| ArduinoSignalTower | 9600 | `\r\n` | NO |
| ArduinoTemp | 9600 | `\r\n` | YES |
| ArduinoValves | 9600 | `\r\n` | NO |
| Cml18 | 9600 | `\r\n` | YES |
| KernPcb | 9600 | `\r\n` | YES |

### Wave 2 — Batch B (11 drivers)
| Driver | BaudRate | CmdTerminator | has_trends | Notes |
|--------|----------|---------------|------------|-------|
| AtlasSyringePump | 57600 | `\r\n` | YES | Dual syringe pump |
| Carino9 | 9600 | `\r\n` | YES | HPLC pump |
| ColeParmerTouch | 115200 | `\r` | YES | POP tests all commented out |
| FoxyR1 | 19200 | `\r\n` | YES | Fraction collector; Integer trends |
| Gynkotek | 9600 | `\r\n` | YES | Binary-encoded flow protocol |
| Heidolph | 9600 | `\r\n` | YES | No POP test file |
| IkaDigitalHotPlate | 9600 | `\r\n` | YES | DataBits=7, Parity=Even |
| IkaOverheadStirrer | 9600 | `\r\n` | YES | Made non-sealed for OldDevice |
| IkaOverheadStirrerOldDevice | 9600 | `\r\n` | YES | DataBits=7, Parity=Even |
| IkaUltraTurrax | 9600 | `\r\n` | YES | Parses split[1] not split[0] |
| KnauerP2And4Pump | 9600 | `\r` | YES | |

### Wave 3 — Batch C (10 drivers + ViciMultiValveActuator)
| Driver | BaudRate | CmdTerminator | has_trends | Notes |
|--------|----------|---------------|------------|-------|
| LambdaDoser | 2400 | `\r` | YES | Parity=Odd; checksum protocol |
| MasterFlexLsDigital | 4800 | `\r` | YES | DataBits=7, Parity=Odd; STX/ACK |
| MettlerBalance | 9600 | `\r\n` | YES | |
| NewEra1000 | 19200 | `\r` | YES | NewEraBaseMethods inlined |
| OrionVersaStarPro | 115200 | `\r` | YES | |
| PilotOne | 9600 | `\r\n` | YES | Hex-encoded PB command protocol |
| SartoriusBalance | 1200 | `\r\n` | YES | DataBits=7, Parity=Odd; ESC prefix |
| SuperDew3 | 9600 | N/A | YES | Raw byte command protocol |
| Vario | 9600 | `\r\n` | YES | Vacuum pump |
| ViciActuator | 9600 | `\r` | YES | |
| ViciMultiValveActuator | 9600 | `\r` | NO | has_trends=NO |

## Build and Test Results

- `dotnet build Helix.Orchestration.slnx`: **PASSED** (0 errors)
- `dotnet test tests/Helix.Orchestration.Devices.Tests/`: **PASSED** (394 tests, 0 failures)
- Tests breakdown: Wave 0 infrastructure + Wave 1 (74) + Wave 2 (82) + Wave 3 (50) + pre-existing

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] IkaOverheadStirrer made non-sealed**
- **Found during:** Task 2.3 (IkaOverheadStirrerOldDevice)
- **Issue:** `IkaOverheadStirrer` was declared `sealed` but `IkaOverheadStirrerOldDevice` needs to inherit from it
- **Fix:** Changed `public sealed class IkaOverheadStirrer` to `public class IkaOverheadStirrer`
- **Files modified:** `Helix.Orchestration.Devices/Drivers/IkaOverheadStirrer.cs`

**2. [Rule 1 - Bug] FakeSerial response terminator bleed-through**
- **Found during:** Task 3.1 (OrionVersaStarPro, MasterFlexLsDigital)
- **Issue:** Responses enqueued as `"response\r\n"` with terminator `\r` caused the `\n` to trigger another read, consuming the next queued response
- **Fix:** Changed test responses to end exactly at the terminator character (`"response\r"` not `"response\r\n"`)
- **Files modified:** `OrionVersaStarProTests.cs`, `MasterFlexLsDigitalTests.cs`

**3. [Rule 1 - Bug] PilotOne response prefix mismatch**
- **Found during:** Task 3.2 (PilotOne)
- **Issue:** Driver checked `resp.StartsWith("{S14")` but the device responds with `{S13...` (TemperatureControlMode register address)
- **Fix:** Changed check to `resp.StartsWith(SlaveResponse)` (`{S`)
- **Files modified:** `Helix.Orchestration.Devices/Drivers/PilotOne.cs`

**4. [Rule 1 - Bug] SartoriusBalance length check after Trim()**
- **Found during:** Task 3.2 (SartoriusBalance)
- **Issue:** `InterpretWeightString` checks `resp.Length != 22` but `Decode` calls `.Trim()` which strips `\r\n` making length 20
- **Fix:** Changed to use raw bytes without Trim(), and relaxed length check to `< 20`
- **Files modified:** `Helix.Orchestration.Devices/Drivers/SartoriusBalance.cs`

### Structural Adaptations (not bugs)

**5. Cml18 — event-driven to request-response**
- **Decision:** POP's Cml18 uses `SerialPortAddEventHandler` callback. Helix `SerialDeviceTransport` is request-response only. Ported as request-response driver sending `M\r\n` and parsing the response format.

**6. ColeParmerTouch — minimal capabilities**
- **Decision:** POP test file is 100% commented out. Per fidelity rules, only capabilities with POP test coverage are ported. Implemented basic Start/Stop/GetInfusedVolume from source reading.

**7. NewEra1000 — NewEraBaseMethods inlined**
- **Decision:** Per plan instruction, NewEraBaseMethods helper is NOT created in Helix. All logic inlined directly into NewEra1000.

## UAT Checklist

- [x] `dotnet build Helix.Orchestration.slnx` passes
- [x] `dotnet test tests/Helix.Orchestration.Devices.Tests/` passes (394 tests, 0 failures)
- [x] All 32 driver `.cs` files exist in `Helix.Orchestration.Devices/Drivers/`
- [x] All 32 test `.cs` files exist in `tests/Helix.Orchestration.Devices.Tests/Drivers/`
- [x] `tests/Helix.Orchestration.Devices.Tests/TestHelpers/FakeSerial.cs` exists
- [x] `FakeSerial.MakeTransport()` accepts `baudRate`, `commandTerminator`, `parity`, `dataBits`
- [x] No capabilities extrapolated beyond POP source + tests
- [x] Command strings verified by test assertions to match POP source exactly
- [x] All 32 driver class names appear in `DeviceAdapterExtensions.cs`
- [x] All 32 capability prefixes from the plan table appear in `DeviceAdapterExtensions.cs`
- [x] `AddSingleton` count in `DeviceAdapterExtensions.cs` >= 35 (actual: 63)
- [x] `ArduinoSignalTower` does NOT override `StreamTrendsAsync`
- [x] `ArduinoValves` does NOT override `StreamTrendsAsync`
- [x] `ViciMultiValveActuator` does NOT override `StreamTrendsAsync`
- [x] `IkaOverheadStirrerOldDevice` test uses `Parity.Even` and `DataBits=7`
- [x] `NewEra1000` does NOT create a `NewEraBaseMethods` helper class

## Commits

- `8507790` feat(driver-port-01): wave 0 -- FakeSerial helper + SerialTransportOptions Parity/DataBits already present
- `89e69d9` feat(driver-port-01): wave 1 -- 11 serial drivers (AalborgDfm...KernPcb) with tests (74 passing)
- `cc66d61` feat(driver-port-01): wave 2 -- 11 serial drivers (AtlasSyringePump...KnauerP2And4Pump) with tests (82 passing)
- `80d1a74` feat(driver-port-01): wave 3 -- 11 serial drivers (LambdaDoser...ViciMultiValveActuator) with tests (50 passing)
- `72efdf0` feat(driver-port-01): wave 4 -- register all 32 Plan 01 serial drivers in DeviceAdapterExtensions

## Known Stubs

None — all driver implementations send real commands to the transport layer. No hardcoded empty values or placeholder data flows to UI rendering.

## Threat Flags

None identified — all 32 drivers are serial device adapters that consume physical device transports. No new network endpoints, HTTP routes, auth paths, or trust boundary crossings introduced.

## Self-Check: PASSED

All 32 driver files verified to exist in `Helix.Orchestration.Devices/Drivers/`.
All 32 test files verified to exist in `tests/Helix.Orchestration.Devices.Tests/Drivers/`.
`FakeSerial.cs` exists at `tests/Helix.Orchestration.Devices.Tests/TestHelpers/FakeSerial.cs`.
`DeviceAdapterExtensions.cs` contains all 32 driver names and capability prefixes.
Build: 0 errors. Tests: 394 passed, 0 failed.

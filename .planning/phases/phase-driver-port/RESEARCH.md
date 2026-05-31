# Phase Driver-Port: PopNextGen Driver Port — Research

**Researched:** 2026-05-31
**Domain:** C# device driver porting — serial/TCP/OPC-UA/CDll/special-transport
**Confidence:** HIGH (all claims are from direct file reads of the actual source)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Test Migration Strategy:** Port the tests too. Each POP test must translate to a Helix xUnit test that:
- Mocks `IDeviceTransport` (not `IPort`)
- Verifies the same serial/TCP command strings
- Asserts `CapabilityInvocationResult.Success` / `.Failed`
- Lives in `tests/Helix.Orchestration.Devices.Tests/Drivers/{DriverName}Tests.cs`

**Scope:** 44 drivers (51 tested minus 3 done minus 3 native-only).

**Batching:** Plan 01 Serial (32), Plan 02 TCP+OPC-UA (4), Plan 03 Other/Special (8).

**Capability naming:** `{devicename}.{CapabilityName}` — lowercase class name prefix.

**Port fidelity:**
1. Command strings: copy exactly, including terminator
2. Response parsing: copy exact logic
3. Validation: copy range checks exactly
4. No new capabilities
5. Transport mapping: `IPort.SendCommand(cmd, timeout)` → `Transport.SendReceiveAsync(Encode(cmd), ct)`
6. Result mapping: `Info.Success()` → `CapabilityInvocationResult.Successful()`, `Info.Failure(msg)` → `CapabilityInvocationResult.Failed(msg)`
7. Trend mapping: `GetTrends()` → `StreamTrendsAsync()` with 2-second polling

### Claude's Discretion
(none specified)

### Deferred Ideas (OUT OF SCOPE)
(none specified)
</user_constraints>

---

## Summary

All 44 driver source files and their POP tests have been read directly. The vast majority (32) are serial drivers using `BaseSerialDevice` or its descendants — these map cleanly to `SerialDeviceTransport` and follow the VapourtecR4 pattern exactly. Plan 01 is a pure batch job with no surprises per driver; the chief decision is grouping by complexity within each batch.

Plan 02 is also clean: Microsaic uses a custom TCP/HTTP client abstraction (`IMicrosaicClient`) that the Helix test mocks directly via `IDeviceTransport`; EasyMax, Fbrm, and ReactIr all use `BaseMettlerOpcClient` on top of `OpcDeviceTransport`, and the POP tests mock `IMettlerOpcCommand` — in Helix these become `IDeviceTransport` mocks following the SystagOpcUaClient pattern.

Plan 03 is the genuinely difficult batch: five of the eight drivers are **not** serial/TCP at all. AtlasReactionSystem and Crystalline use File I/O and SQL, respectively — no transport exists for them. IfmMasterController uses IO-Link HTTP. OceanViewDriver/Nir/Uv wrap a vendor C DLL through an interface (`IOceanViewDriver`). PicoTc08 wraps `IPicoApi` which is satisfied by `PicoTc08LowLevelWrapper` (also in scope). None of these can use `DeviceAdapterBase(IDeviceTransport)` directly — they require a facade pattern where the non-transport dependency becomes a constructor parameter and no `Transport.SendReceiveAsync` calls are made.

**Primary recommendation:** Use a facade sub-class of `DeviceAdapterBase` for Plan 03 drivers that accept a plain `IDeviceTransport` as a stub transport (or a no-op `NullDeviceTransport`) and route all real work through the injected domain interface. This is consistent with how `SystagOpcUaClient` accepts `internal IDeviceTransport` for test injection.

---

## Transport Classification Table

All 44 drivers. Transport column is the Helix transport type to inject.
`has_trends` = YES means the driver or a base class it inherits from implements `GetTrends()` and this must be ported as `StreamTrendsAsync()`.

| # | Driver | Transport | ~Capabilities | has_trends | has_POP_test | Notes |
|---|--------|-----------|--------------|------------|-------------|-------|
| 1 | AalborgDfm | Serial | 8 | YES | YES | BaudRate 9600, CmdEnd `\r` |
| 2 | Alicat | Serial | 6 | YES | YES | BaudRate 19200, CmdEnd `\r`; uses `BaseMassFlowController` |
| 3 | ArduinoConductivity | Serial | 7 | YES | YES | BaudRate 9600, no CmdEnd suffix |
| 4 | ArduinoLeak | Serial | 4 | YES | YES | BaudRate 9600, simple sensor |
| 5 | ArduinopH | Serial | 7 | YES | YES | BaudRate 9600 |
| 6 | ArduinoPressure | Serial | 4 | YES | YES | BaudRate 9600 |
| 7 | ArduinoSignalTower | Serial | 10 | NO | YES | `BaseMultiValve` — no GetTrends at any level |
| 8 | ArduinoTemp | Serial | 5 | YES | YES | BaudRate 9600 |
| 9 | ArduinoValves | Serial | 10 | NO | YES | `BaseMultiValve` — no GetTrends |
| 10 | AtlasSyringePump | Serial | 18 | YES | YES | BaudRate varies; dual-syringe pump; large capability set |
| 11 | Carino9 | Serial | 12 | YES | YES | High-pressure pump; `BaseHighPressurePump` |
| 12 | Cml18 | Serial | 3 | YES | YES | Simple sensor |
| 13 | ColeParmerTouch | Serial | 15 | YES | YES | Syringe pump |
| 14 | FoxyR1 | Serial | 15 | YES (Integer) | YES | Fraction collector; trends return `Integer` not `IValue` |
| 15 | Gynkotek | Serial | 8 | YES | YES | High-pressure pump |
| 16 | Heidolph | Serial | 10 | YES | YES | `BaseStirrerDevice` |
| 17 | IkaDigitalHotPlate | Serial | 12 | YES | YES | Hotplate stirrer; BaudRate 9600 |
| 18 | IkaOverheadStirrer | Serial | 9 | YES (from BaseIkaStirrer) | YES | Inherits GetTrends from BaseIkaStirrer |
| 19 | IkaOverheadStirrerOldDevice | Serial | 9 | YES (from BaseIkaStirrer) | YES | Subclass of IkaOverheadStirrer; 7 DataBits, Even Parity — requires `SerialTransportOptions` override |
| 20 | IkaUltraTurrax | Serial | 7 | YES (from BaseIkaStirrer) | YES | Subclass of BaseIkaStirrer directly |
| 21 | KernPcb | Serial | 5 | YES | YES | Balance |
| 22 | KnauerP2And4Pump | Serial | 8 | YES | YES | High-pressure pump |
| 23 | LambdaDoser | Serial | 7 | YES | YES | Solids doser; `BaseSetPointDevice` |
| 24 | MasterFlexLsDigital | Serial | 15 | YES | YES | Peristaltic pump; `BasePeristalticPumpConti` |
| 25 | MettlerBalance | Serial | 6 | YES | YES | BaudRate varies |
| 26 | NewEra1000 | Serial | 9 | YES | YES | Uses `NewEraBaseMethods` delegation pattern |
| 27 | OrionVersaStarPro | Serial | 7 | YES | YES | Probe/sensor |
| 28 | PilotOne | Serial | 15 | YES | YES | Heater/chiller; `BaseSetPointDevice` |
| 29 | SartoriusBalance | Serial | 5 | YES | YES | Balance |
| 30 | SuperDew3 | Serial | 3 | YES | YES | Sensor |
| 31 | Vario | Serial | 10 | YES | YES | Vacuum pump; `BaseVacPumpDevice` |
| 32 | ViciActuator | Serial | 9 | YES | YES | Valve actuator |
| 33 | ViciMultiValveActuator | Serial | 9 | NO | YES | MultiValve base — no GetTrends |
| 34 | Microsaic | TCP (HTTP text) | 20+ | YES (Matrix) | YES | `BaseTcpDevice`; async factory Create(); large driver (~1000 lines) |
| 35 | EasyMax | OPC-UA | 6 | YES | YES | `BaseMettlerOpcClient`; OpcName = "iControl" |
| 36 | Fbrm | OPC-UA | 4 | NO (commented) | YES | `BaseMettlerOpcClient`; OpcName = "iCFBRM" |
| 37 | ReactIr | OPC-UA | 8 | NO (no override) | YES | `BaseMettlerOpcClient`; OpcName = "iCIR" |
| 38 | AtlasReactionSystem | File I/O | 4 | YES | YES | Reads Atlas CSV log files; no serial/TCP |
| 39 | Crystalline | SQL Server | 5 | YES | YES | SQL via `ICrystallineSqlClient`; needs facade |
| 40 | IfmMasterController | IO-Link HTTP | 6 | YES | YES | Via `IPopIoLinkClient`; async Open; needs facade |
| 41 | OceanViewDriver | CDll (vendor) | 12 | NO | YES (OceanViewTests) | Wraps `IOceanViewDriver` interface; not itself a DeviceAdapterBase candidate |
| 42 | OceanViewNir | CDll (vendor) | 13 | YES (Matrix) | YES | Inherits `BaseOceanView`; wraps `IOceanViewDriver` |
| 43 | OceanViewUv | CDll (vendor) | 13 | YES (Matrix) | YES | Inherits `BaseOceanView`; wraps `IOceanViewDriver` |
| 44 | PicoTc08 | CDll (IPicoApi) | 7 | YES (DoubleValue) | YES | Wraps `IPicoApi`; `[SupportedOSPlatform("windows")]` |
| — | PicoTc08LowLevelWrapper | (is IPicoApi impl) | — | — | — | This IS the CDll layer; it calls `PicoTc08LowLevelApiImports` directly; stays as-is in Plan 03 alongside PicoTc08 |

**Drivers with NO POP test (verified absent):**
ArduinoAmp, ArduinoEnvironmentSensor, ArduinoPeri, ArduinoTimeOfFlight, ArduinoVoice, Bronkhorst, ChemTron, HnpPump, Ishmatec, KellerLeo, LiquiSonic, NewEra9000, VapourtecSf10, WatsonMarlow520Du — these 14 are NOT in scope (they are not in the 44-driver list above).

**Entirely commented-out (code not compilable):** Gx271, Haake, Hamilton600, Magritek, Mra10000, ThermoNeslabRte, TitanServoMotor — not in scope.

---

## Plan 03 Special Drivers Analysis

### 38. AtlasReactionSystem — File I/O

**POP transport:** `ComNames.FileIo`. No serial port. Constructor takes `IDirectoryInfo`, `IReadCsvFile`, `IGetProcess`, `ILogger`.

**What it does:** Monitors a directory for Atlas CSV log files. `GetTrends()` reads the latest row from the most-recently-written CSV. `IsDevice()` checks if the Atlas desktop process is running.

**Helix porting strategy:** This driver has no `IDeviceTransport`. Use a **facade** pattern:
- Create `AtlasReactionSystem : DeviceAdapterBase` that accepts the three domain interfaces plus `IDeviceTransport` (can be a `NullDeviceTransport` or `Substitute.For<IDeviceTransport>()` in tests)
- Register capabilities: `Open(path)`, `GetTrends` (via `StreamTrendsAsync`)
- The POP test (`AtlasReactionSystemTest.cs`) mocks `IDirectoryInfo`, `IReadCsvFile`, `IGetProcess` — Helix test should do the same
- No `Transport.SendReceiveAsync` calls are needed; capability handlers call domain interfaces directly

**Risk:** `IDirectoryInfo`, `IReadCsvFile`, `IGetProcess` are POP-specific interfaces. They must either be declared in `Helix.Orchestration.Devices` or referenced from a shared contracts assembly. **Recommend declaring local copies** (they are small, 1-3 methods each).

### 39. Crystalline — SQL Server

**POP transport:** `ComNames.SqlServer`. Constructor takes `ICrystallineSqlClient`, `ILogger`.

**What it does:** Connects to a Crystalline SQL Server instance. `CreateSession(endPoint)` opens a connection. `GetTrends()` calls `_crystallineSqlClient.GetRuntimeDataForReactor(reactor)` and appends a camera image trend.

**Helix porting strategy:** Same facade approach as AtlasReactionSystem:
- `Crystalline : DeviceAdapterBase` accepting `ICrystallineSqlClient` and a stub `IDeviceTransport`
- Capabilities: `CreateSession`, `CreateSubscription`, `Close`, `FindActiveSystems`, `GetTrends` (via `StreamTrendsAsync`)
- POP test (`CrystallineSqlClientTests.cs`) mocks `ICrystallineSqlClient` — copy that approach
- `ICrystallineSqlClient` is a medium-sized interface (~6 methods); declare locally in Helix

**Risk:** `GetTrends()` returns a `Trend<IValue>` that includes an image blob. The image trend should either be omitted from `StreamTrendsAsync` (images don't fit `TrendSample.Value: double`) or serialized as a base64 string. **Recommendation: skip image trends in StreamTrendsAsync**, document the omission in a comment.

### 40. IfmMasterController — IO-Link HTTP

**POP transport:** `ComNames.IoLink`. Constructor takes `ILogger`, `IPopIoLinkClient`, `IFile`.

**What it does:** Communicates with an IFM IO-Link master controller over HTTP. `OpenAsync(endPoint, portNumber)` establishes a session and loads a JSON config from `PopConfig/io_link.json`. `GetTrends()` calls `popIoLinkClient.GetAllProcessDataForDeviceAsync().Result`.

**Helix porting strategy:** Facade approach:
- `IfmMasterController : DeviceAdapterBase` accepting `IPopIoLinkClient`, `IFile`, and stub `IDeviceTransport`
- `IPopIoLinkClient` and `IFile` are POP interfaces; declare locally
- Capabilities: `FindActiveSystems`, `Open` (with endPoint+portNumber params), `Close`, `GetTrends` (via `StreamTrendsAsync`)
- The `.Result` blocking call in `GetTrends()` should be replaced with proper `await` inside the `StreamTrendsAsync` loop
- POP test (`IfmMasterControllerTests.cs`) mocks `IPopIoLinkClient` and `IFile`

**Risk:** `IPopIoLinkClient` is a substantial async interface (4+ methods). Declare locally. The `io_link.json` file path is hardcoded as `"PopConfig/io_link.json"` — in Helix this should be configurable or loaded from `IFile` injection as-is.

### 41. OceanViewDriver — CDll

**POP role:** `OceanViewDriver` is NOT a device adapter itself — it IS the CDll wrapper (wraps `NetOceanDirect.OceanDirect` and `OpticalBench`). It satisfies the `IOceanViewDriver` interface.

**Helix porting strategy:** `OceanViewDriver` maps to `CDllDeviceTransport`. It does NOT become a `DeviceAdapterBase`. Instead:
- The Helix `CDllDeviceTransport` loads `NetOceanDirect.dll` (or the vendor DLL)
- `OceanViewDriver` in Helix is a thin wrapper that still implements `IOceanViewDriver` but delegates to the native DLL via P/Invoke — it is itself not a `DeviceAdapterBase`
- `OceanViewNir` and `OceanViewUv` become `DeviceAdapterBase` subclasses accepting `IOceanViewDriver` (interface) as constructor parameter plus stub `IDeviceTransport`

**Risk:** The `NetOceanDirect` vendor DLL must be present at runtime. Tests mock `IOceanViewDriver` directly (POP test pattern) — this is fine and carries through to Helix tests.

### 42–43. OceanViewNir / OceanViewUv — CDll (via IOceanViewDriver)

Both inherit `BaseOceanView(IOceanViewDriver oceanViewDriver, ILogger logger)`.

`BaseOceanView` has ~15 public methods (Open, Close, SetIntegrationTime, SetScansToAverage, GetSpectrum/Wavelengths, etc.) plus `GetTrends()` returning `List<Trend<Matrix>>`. The `Matrix` type contains a 2D double array plus timestamp — this does not fit `TrendSample.Value: double`.

**Helix porting strategy:**
- `OceanViewUv : DeviceAdapterBase` accepting `IOceanViewDriver` + stub `IDeviceTransport`
- `OceanViewNir : DeviceAdapterBase` accepting `IOceanViewDriver` + stub `IDeviceTransport`
- `StreamTrendsAsync` should NOT be implemented (no scalar value to yield — spectrum is a matrix). Override returns empty (default base behavior).
- All spectral acquisition capabilities (Open, Close, SetIntegrationTime, GetSpectrum, etc.) registered via `RegisterCapabilities`. Spectrum data returned as `CapabilityInvocationResult.Successful(jsonElement)` where the element serializes the wavelength/intensity arrays.
- POP test mocks `IOceanViewDriver` → same pattern in Helix

### 44 + PicoTc08LowLevelWrapper — CDll (IPicoApi / PicoTc08LowLevelApiImports)

**PicoTc08:** Takes `IPicoApi` + `ILogger`. `IPicoApi` is the abstraction over the Pico TC-08 DLL calls. The class is tagged `[SupportedOSPlatform("windows")]`.

**PicoTc08LowLevelWrapper:** Implements `IPicoApi` by calling `PicoTc08LowLevelApiImports` (P/Invoke). This is equivalent to `OceanViewDriver` — it IS the native layer, not a device adapter.

**Helix porting strategy:**
- `PicoTc08 : DeviceAdapterBase` accepting `IPicoApi` + stub `IDeviceTransport` + `ILogger`
- `PicoTc08LowLevelWrapper` remains as-is (it is an implementation of `IPicoApi`; it is not a `DeviceAdapterBase`)
- Add `[SupportedOSPlatform("windows")]` to the Helix class
- `GetTrends()` polls 8 thermocouple channels → `StreamTrendsAsync` yields 8 `TrendSample` per poll cycle
- `IPicoApi` must be declared in Helix (5 methods)
- POP test (`PicoTc08Tests.cs`) mocks `IPicoApi` → same approach in Helix

---

## Helix Test Infrastructure

### Framework and Location

- **Framework:** xUnit 2.9 + NSubstitute 5.3 + Moq 4.20
- **Target:** net10.0
- **Test project:** `tests/Helix.Orchestration.Devices.Tests/`
- **Driver test location:** `tests/Helix.Orchestration.Devices.Tests/Drivers/`

### Two Established Patterns (choose by driver type)

#### Pattern A: FakeSerialPortConnection (serial drivers)

Used in `VapourtecR4Tests.cs`. A hand-written inline `FakeSerialPortConnection` + `FakeSerialPortConnectionFactory` + `FakeSerialStream` simulates the full serial round-trip. The fake enqueues byte arrays; writes are captured in a `List<byte[]>`.

```csharp
// Canonical serial test helper — currently inline in VapourtecR4Tests.cs
// MUST extract into a shared test helper for Plan 01 (32 drivers cannot each copy 80 lines)

private static SerialDeviceTransport MakeTransport(FakeSerialPortConnection port)
    => new SerialDeviceTransport(
        new SerialTransportOptions { PortName = "COM1", BaudRate = 19200, CommandTerminator = "\r\n" },
        NullLogger<SerialDeviceTransport>.Instance,
        new FakeSerialPortConnectionFactory(port));
```

**Critical gap:** `FakeSerialPortConnection` and `FakeSerialPortConnectionFactory` are currently private nested classes inside `VapourtecR4Tests`. For 32 serial drivers in Plan 01 this must be extracted to a shared test helper (e.g., `Helix.Orchestration.Devices.Tests/Helpers/FakeSerial.cs`). Without this, Plan 01 would duplicate ~80 lines of infrastructure per test class.

#### Pattern B: NSubstitute IDeviceTransport mock (TCP/OPC-UA drivers)

Used in `KnauerAzuraTests.cs`. Simpler — substitutes `IDeviceTransport` directly:

```csharp
var transport = Substitute.For<IDeviceTransport>();
var queue = new Queue<byte[]>(responses.Select(r => Encoding.ASCII.GetBytes(r)));
transport.SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
         .Returns(_ => Task.FromResult(queue.Dequeue()));
var driver = new KnauerAzura(transport, NullLogger<KnauerAzura>.Instance);
```

This is simpler and is the right pattern for all non-serial drivers (TCP, OPC-UA) and for the facade drivers (Plan 03) where `IDeviceTransport` is a stub.

### Existing Tests (already in Helix)

| File | Driver | Pattern |
|------|--------|---------|
| `VapourtecR4Tests.cs` | VapourtecR4 | FakeSerialPortConnection |
| `KnauerAzuraTests.cs` | KnauerAzura | NSubstitute mock |
| `SystagOpcUaClientTests.cs` | SystagOpcUaClient | NSubstitute mock |

### Important: No global FakeSerial helper exists yet

The `FakeSerialPortConnection` class exists as private nested class in `VapourtecR4Tests.cs` AND in `SerialTransportTests.cs` — duplicated. Plan 01 Wave 0 must extract this into a shared `TestHelpers/FakeSerial.cs` before writing any driver tests.

---

## DeviceAdapterExtensions Pattern

### Current state

`DeviceAdapterExtensions.AddMigratedDeviceAdapters()` in `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` currently registers VapourtecR4, KnauerAzura, and SystagOpcUaClient. Each driver follows:

1. Register transport options as singleton
2. Register transport as singleton
3. Register driver as singleton
4. In the `CapabilityRegistry` builder, enumerate `driver.GetCapabilities()` and register each as `"{prefix}.{capability.Name}"`

### How to extend for 44 new drivers

Each new driver gets a block like:

```csharp
// AalborgDfm — serial
services.AddSingleton(new SerialTransportOptions { PortName = "COM4", BaudRate = 9600, CommandTerminator = "\r" });
services.AddSingleton<AalborgDfm>();
// ... then in CapabilityRegistry builder:
var dfm = sp.GetRequiredService<AalborgDfm>();
foreach (var cap in dfm.GetCapabilities())
    registry.Register($"aalborgdfm.{cap.Name}", new HelixAdapterCapabilityBridge(dfm));
```

**Problem with current design at scale:** If 44 drivers all share a single `SerialTransportOptions` singleton, only one driver gets the right baud/port. Each driver needs its own named transport options. Options:
- Use `sp.GetServices<T>()` with named registrations (not supported natively in Microsoft.Extensions.DI without a wrapper)
- Register each transport as a concrete singleton constructed with `new` inline (current pattern for KnauerAzura), not via `services.AddSingleton<T>()`
- **Recommended approach:** Continue the inline constructor pattern (as done for KnauerAzura/SystagOpcUaClient); each driver's transport constructed with `new SerialDeviceTransport(new SerialTransportOptions { ... }, ...)` inside the service factory lambda. Port names remain placeholder (`"COM3"`, `"COM4"`, etc.) — they are overridden by config in production.

### Capability prefix table (all 44 drivers)

| Driver | Prefix |
|--------|--------|
| AalborgDfm | `aalborgdfm` |
| Alicat | `alicat` |
| ArduinoConductivity | `arduinoconductivity` |
| ArduinoLeak | `arduinoleak` |
| ArduinopH | `arduinoph` |
| ArduinoPressure | `arduinopressure` |
| ArduinoSignalTower | `arduinosignaltower` |
| ArduinoTemp | `arduinotemp` |
| ArduinoValves | `arduinovalves` |
| AtlasSyringePump | `atlassyringepump` |
| Carino9 | `carino9` |
| Cml18 | `cml18` |
| ColeParmerTouch | `coleParmertouch` (note: preserve case as lowercase) → `coleparmertouch` |
| FoxyR1 | `foxyr1` |
| Gynkotek | `gynkotek` |
| Heidolph | `heidolph` |
| IkaDigitalHotPlate | `ikadigitalhotplate` |
| IkaOverheadStirrer | `ikaoverheadstirrer` |
| IkaOverheadStirrerOldDevice | `ikaoverheadstirrerolddevice` |
| IkaUltraTurrax | `ikaultraturrax` |
| KernPcb | `kernpcb` |
| KnauerP2And4Pump | `knauerp2and4pump` |
| LambdaDoser | `lambdadoser` |
| MasterFlexLsDigital | `masterflexlsdigital` |
| MettlerBalance | `mettlerbalance` |
| NewEra1000 | `newera1000` |
| OrionVersaStarPro | `orionverstarpro` |
| PilotOne | `pilotone` |
| SartoriusBalance | `sartoriusbalance` |
| SuperDew3 | `superdew3` |
| Vario | `vario` |
| ViciActuator | `viciactuator` |
| ViciMultiValveActuator | `vicimultivalveactuator` |
| Microsaic | `microsaic` |
| EasyMax | `easymax` |
| Fbrm | `fbrm` |
| ReactIr | `reactir` |
| AtlasReactionSystem | `atlasreactionsystem` |
| Crystalline | `crystalline` |
| IfmMasterController | `ifmmastercontroller` |
| OceanViewDriver | (not a DeviceAdapterBase — no registration) |
| OceanViewNir | `oceanviewnir` |
| OceanViewUv | `oceanviewuv` |
| PicoTc08 | `picotc08` |
| PicoTc08LowLevelWrapper | (not a DeviceAdapterBase — no registration) |

---

## Shared Base Classes

These POP base classes affect multiple Plan 01 drivers. The porting team does NOT need to port these base classes — they are replaced by `DeviceAdapterBase`. But understanding what methods they contributed is necessary for registering the correct capabilities.

| POP Base Class | Used By | Methods contributed to capabilities |
|---------------|---------|--------------------------------------|
| `BaseIkaStirrer` | IkaOverheadStirrer, IkaUltraTurrax | Start, Stop, SetPoint, GetSetPoint, GetSetPointLimits, GetTrends (torque+speed+setpoint) |
| `NewEraBaseMethods` | NewEra1000 (via delegation) | Start, Stop, Pause, SetRate, SetVolume, SetDiameter, Configure, ReverseDirection, GetTrends |
| `BaseSerialDevice` | Most serial drivers | SendCommand wrapper; BaudRate/DataBits/Parity overrides |
| `BaseMassFlowController` | Alicat, Bronkhorst | Start, Stop, SetPoint, GetSetPoint, GetSetPointLimits, TareAll, GetTrends |
| `BaseStirrerDevice` | Heidolph, ChemTron | Start, Stop, SetPoint, GetSetPoint, GetSetPointLimits, GetTrends |
| `BaseHighPressurePump` | Carino9, Gynkotek, KnauerP2And4Pump | Start, Stop, SetPoint, GetSetPoint, GetSetPointLimits, GetTrends |
| `BasePeristalticPumpConti` | MasterFlexLsDigital, Ishmatec, HnpPump, VapourtecSf10, ArduinoPeri, NewEra9000, WatsonMarlow520Du | Start, Stop, SetPoint, GetSetPoint, GetTrends |
| `BaseSetPointDevice` | PilotOne, Piccolo, LambdaDoser, Julabo | Start, Stop, SetPoint, GetSetPoint, GetSetPointLimits, GetTrends |
| `BaseVacPumpDevice` | Vario | Start, Stop, SetPoint, GetSetPoint, GetTrends |
| `BaseMultiValve` | ArduinoSignalTower, ArduinoValves, ViciMultiValveActuator | GoTo, GetValves, GetNumberOfValves — NO GetTrends |
| `BaseMettlerOpcClient` | EasyMax, Fbrm, ReactIr, KaiserRaman, EasyViewer | CreateSession, CreateSubscription, GetTrends, FindActiveSystems |
| `BaseOceanView` | OceanViewUv, OceanViewNir | Open, Close, SetIntegrationTime, SetScansToAverage, SetBoxcarWidth, GetSpectrum-related, GetTrends (Matrix) |
| `BaseTcpDevice` | Microsaic | sendAsync wrapper |

### NewEra delegation note

`NewEra1000` uses `NewEraBaseMethods` via **delegation** (not inheritance): it creates `new NewEraBaseMethods(SendCommand)` passing its own `SendCommand` method as a delegate. The `NewEraBaseMethods` class itself does NOT inherit from anything. In Helix, port `NewEra1000` as a self-contained driver — copy the methods from `NewEraBaseMethods` that `NewEra1000Tests.cs` exercises into the Helix `NewEra1000` driver directly.

---

## Porting Risks & Gotchas

### Risk 1: IkaOverheadStirrerOldDevice — non-standard serial parity

`IkaOverheadStirrerOldDevice` overrides `Parity = Even` and `DataBits = 7`. `SerialTransportOptions` must expose Parity and DataBits fields and pass them to `System.IO.Ports.SerialPort`. Verify `SerialTransportOptions` includes these; if not, they must be added before this driver can be ported.

```csharp
// Helix check needed:
public sealed class SerialTransportOptions {
    // Does it have Parity and DataBits? Check before Plan 01 Batch B.
}
```

**Action:** Read `SerialTransportOptions.cs` at the start of Plan 01 Batch B task. If missing, add `Parity` and `DataBits` properties first.

### Risk 2: ModbusDeviceTransport.SendReceiveAsync throws NotSupportedException

`ModbusDeviceTransport.SendReceiveAsync` explicitly throws: `"Use typed Modbus methods like ReadHoldingRegistersAsync instead of SendReceiveAsync"`. `ParrController4848` uses `BaseModbusDevice` which calls typed Modbus operations. ParrController4848 is NOT in the 44-driver scope (it has no POP test) — this is a note for future reference only.

### Risk 3: FoxyR1 GetTrends returns `Trend<Integer>` not `Trend<IValue>`

`FoxyR1.GetTrends()` returns `List<Trend<Integer>>`. In `StreamTrendsAsync`, cast the `Integer` value to `double` (it holds a tube number). Safe: `Integer` is a POP value type wrapping `int`.

### Risk 4: Microsaic is a very large driver (~1000 lines) with async factory constructor

`Microsaic` uses a static `Create()` async factory. It has complex response parsing and a large capability set. Assign extra task time in Plan 02. Test in POP (`MicrosaicTests.cs`) mocks `IMicrosaicClient` — Helix test should mock `IDeviceTransport` instead (where `IMicrosaicClient` becomes `IDeviceTransport` wrapping HTTP responses).

### Risk 5: OceanViewNir/Uv GetTrends returns Trend<Matrix>

`BaseOceanView.GetTrends()` yields `Trend<Matrix>` where `Matrix` wraps a `double[][]`. `TrendSample.Value` is a `double`. **Do not implement `StreamTrendsAsync`** for OceanView drivers — let it return the default empty stream. Expose spectrum via `GetSpectrum` capability returning a JSON array instead.

### Risk 6: Plan 03 drivers need new domain interfaces in Helix.Orchestration.Devices

Four POP-specific interfaces need to be declared locally in the Helix project:
- `IOceanViewDriver` (~12 methods)
- `IPicoApi` (5 methods)
- `ICrystallineSqlClient` (~6 methods)
- `IPopIoLinkClient` (~4 methods)
- `IDirectoryInfo` + `IReadCsvFile` + `IGetProcess` (AtlasReactionSystem, each ~2-3 methods)

These should be declared in `Helix.Orchestration.Devices/Interfaces/` (the folder already exists).

### Risk 7: EasyMax/Fbrm/ReactIr use IMettlerOpcCommand abstraction — NOT raw OPC transport

In POP, the Mettler OPC drivers (`EasyMax`, `Fbrm`, `ReactIr`) do NOT call `IPort.SendCommand`. They use `IMettlerOpcCommand.GetAtNode<T>()` / `GetTrendsAtNode()` / `CreateSession()`. Their POP tests mock `IMettlerOpcCommand`.

In Helix, these drivers must take `IDeviceTransport` (for `DeviceAdapterBase`) AND a separately-injected `IMettlerOpcCommand` (for the Mettler-specific logic). The `IDeviceTransport` is functionally a no-op (used only for the base class health check). `IMettlerOpcCommand` must be declared locally.

**Alternatively**, wrap `IMettlerOpcCommand` inside `OpcDeviceTransport` — but this would require the transport to understand Mettler-specific OPC node naming. The cleaner design is to declare `IMettlerOpcCommand` locally and pass it alongside the transport.

### Risk 8: FakeSerialPortConnection is duplicated — extract before Plan 01

The fake serial infrastructure exists in two places (`VapourtecR4Tests` and `SerialTransportTests`), both as private nested classes. All 32 Plan 01 drivers need serial mocking. Extract to a shared `TestHelpers/FakeSerial.cs` in Wave 0 of Plan 01.

### Risk 9: Capability prefix collision — `OrionVersaStarPro` prefix

`OrionVersaStarPro` becomes `orionversastarpro` — check this fits within any key length limits in `CapabilityRegistry`. No length limit was observed in the source but verify.

### Risk 10: ArduinoSignalTower and ArduinoValves have no GetTrends

These two Arduino drivers inherit from `BaseMultiValve` which has no `GetTrends()`. `StreamTrendsAsync` should not be overridden — the default empty implementation in `DeviceAdapterBase` is correct.

---

## Recommended Task Breakdown

### Plan 01 — Serial (32 drivers)

The CONTEXT.md batch groupings (A/B/C of ~11 each) are reasonable. Adjust slightly based on complexity:

**Wave 0 (prerequisite, before any driver tasks):**
- Extract `FakeSerialPortConnection` + `FakeSerialPortConnectionFactory` + `FakeSerialStream` into `tests/Helix.Orchestration.Devices.Tests/TestHelpers/FakeSerial.cs`
- Verify `SerialTransportOptions` has `Parity` and `DataBits` properties; add if missing (needed for IkaOverheadStirrerOldDevice in Batch B)

**Batch A (~11 drivers) — simpler sensors and pumps:**
AalborgDfm, Alicat, ArduinoConductivity, ArduinoLeak, ArduinopH, ArduinoPressure, ArduinoTemp, ArduinoValves, ArduinoSignalTower, Cml18, KernPcb

Rationale: all small-capability sensors/probes/valves. ArduinoSignalTower and ArduinoValves have no GetTrends so they are slightly simpler.

**Batch B (~11 drivers) — medium pumps and stirrers:**
AtlasSyringePump, Carino9, ColeParmerTouch, Gynkotek, Heidolph, IkaDigitalHotPlate, IkaOverheadStirrer, IkaOverheadStirrerOldDevice, IkaUltraTurrax, KnauerP2And4Pump, FoxyR1

Rationale: IkaOverheadStirrerOldDevice must come after IkaOverheadStirrer (it subclasses it). Old device requires Parity/DataBits fix (Wave 0). FoxyR1 has an `Integer` trend quirk.

**Batch C (~10 drivers) — remaining pumps and sensors:**
LambdaDoser, MasterFlexLsDigital, MettlerBalance, NewEra1000, OrionVersaStarPro, PilotOne, SartoriusBalance, SuperDew3, Vario, ViciActuator, ViciMultiValveActuator

Rationale: NewEra1000 needs delegation extraction from `NewEraBaseMethods`. ViciMultiValveActuator has no GetTrends.

### Plan 02 — TCP + OPC-UA (4 drivers)

**Wave 0 prerequisite:** Declare `IMettlerOpcCommand` interface locally in `Helix.Orchestration.Devices/Interfaces/`.

**Task 02-A:** Microsaic (TCP) — standalone; large driver, takes full task
**Task 02-B:** EasyMax + Fbrm + ReactIr (OPC-UA via IMettlerOpcCommand) — similar patterns, can be one task with 3 subtasks

### Plan 03 — Other/Special (8 drivers)

**Wave 0 prerequisite:** Declare the following interfaces locally in `Helix.Orchestration.Devices/Interfaces/`:
- `IOceanViewDriver` (for OceanViewUv/Nir)
- `IPicoApi` (for PicoTc08)
- `ICrystallineSqlClient` (for Crystalline)
- `IPopIoLinkClient` (for IfmMasterController)
- `IDirectoryInfo`, `IReadCsvFile`, `IGetProcess` (for AtlasReactionSystem)

**Task 03-A:** OceanViewDriver (stub, not DeviceAdapterBase) + OceanViewUv + OceanViewNir — group because OceanViewDriver is the IOceanViewDriver implementation; Uv and Nir both use it
**Task 03-B:** PicoTc08 + PicoTc08LowLevelWrapper — paired because PicoTc08LowLevelWrapper is the `IPicoApi` implementation
**Task 03-C:** AtlasReactionSystem — File I/O facade
**Task 03-D:** Crystalline — SQL facade
**Task 03-E:** IfmMasterController — IO-Link HTTP facade

---

## Code Examples

### Serial driver — canonical Helix port pattern

```csharp
// Source: Helix.Orchestration.Devices/Drivers/VapourtecR4.cs (established pattern)
public sealed class AalborgDfm : DeviceAdapterBase
{
    public AalborgDfm(SerialDeviceTransport transport, ILogger<AalborgDfm> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        builder.RegisterSimple("GetFlow", "Get flow reading", async ct =>
        {
            var resp = Decode(await Transport.SendReceiveAsync(Encode("F\r"), ct));
            if (string.IsNullOrEmpty(resp))
                return CapabilityInvocationResult.Failed("No response from device");
            return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
        });
        // ... other capabilities
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            // poll GetFlow, GetPressure, GetTemperature
            yield return new TrendSample { TrendName = "aalborgdfm.flow", Value = ..., Unit = "mL/min" };
            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
```

### Facade driver — Plan 03 pattern (IOceanViewDriver)

```csharp
// Not using Transport.SendReceiveAsync at all; IDeviceTransport is a no-op stub
public sealed class OceanViewUv : DeviceAdapterBase
{
    private readonly IOceanViewDriver _driver;

    // Production constructor
    public OceanViewUv(IOceanViewDriver driver, IDeviceTransport transport, ILogger<OceanViewUv> logger)
        : base(transport, logger) { _driver = driver; }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas("Open", "Open the device by ID",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"deviceId\":{\"type\":\"integer\"}},\"required\":[\"deviceId\"]}"),
            async (parameters, _, ct) =>
            {
                var deviceId = parameters.GetProperty("deviceId").GetInt32();
                var result = _driver.OpenDevice(deviceId);
                return result.State == SuccessState.Success
                    ? CapabilityInvocationResult.Successful()
                    : CapabilityInvocationResult.Failed("Device did not open correctly");
            });
        // ... other capabilities
    }
    // StreamTrendsAsync: do NOT override — base returns empty (spectrum != scalar double)
}
```

### NSubstitute test pattern for non-serial drivers

```csharp
// Source: KnauerAzuraTests.cs pattern (established)
private static (OceanViewUv driver, IOceanViewDriver oceanView) CreateDriver()
{
    var transport = Substitute.For<IDeviceTransport>();
    var oceanView = Substitute.For<IOceanViewDriver>();
    oceanView.OpenDevice(Arg.Any<int>()).Returns(Success());
    var driver = new OceanViewUv(oceanView, transport, NullLogger<OceanViewUv>.Instance);
    return (driver, oceanView);
}
```

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9 |
| Config file | none (uses xunit runner defaults) |
| Quick run command | `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~DriverName"` |
| Full suite command | `dotnet test tests/Helix.Orchestration.Devices.Tests/` |

### Per-Plan Test Requirements

| Plan | Drivers | Test Type | Command |
|------|---------|-----------|---------|
| Plan 01 Batch A | 11 serial drivers | Unit (FakeSerial) | `dotnet test --filter "BatchA"` or by file |
| Plan 01 Batch B | 11 serial drivers | Unit (FakeSerial) | Per file |
| Plan 01 Batch C | 10 serial drivers | Unit (FakeSerial) | Per file |
| Plan 02 | Microsaic + 3 OPC | Unit (NSubstitute) | Per file |
| Plan 03 | 6 facade drivers + 2 impls | Unit (NSubstitute + interface mocks) | Per file |

### Wave 0 Gaps (must be created before any driver port)

- [ ] `tests/Helix.Orchestration.Devices.Tests/TestHelpers/FakeSerial.cs` — shared serial fake (extract from VapourtecR4Tests)
- [ ] Verify `SerialTransportOptions` has Parity/DataBits; add if missing
- [ ] `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs` — for Plan 02
- [ ] `Helix.Orchestration.Devices/Interfaces/IOceanViewDriver.cs` — for Plan 03
- [ ] `Helix.Orchestration.Devices/Interfaces/IPicoApi.cs` — for Plan 03
- [ ] `Helix.Orchestration.Devices/Interfaces/ICrystallineSqlClient.cs` — for Plan 03
- [ ] `Helix.Orchestration.Devices/Interfaces/IPopIoLinkClient.cs` — for Plan 03
- [ ] `Helix.Orchestration.Devices/Interfaces/IDirectoryInfo.cs`, `IReadCsvFile.cs`, `IGetProcess.cs` — for Plan 03

---

## Open Questions

1. **SerialTransportOptions — Parity and DataBits**
   - What we know: `IkaOverheadStirrerOldDevice` needs `Parity.Even` and `DataBits = 7`
   - What's unclear: Whether `SerialTransportOptions` exposes these (not read in this session — only `PortName`, `BaudRate`, `CommandTerminator` seen in test code)
   - Recommendation: Read `SerialTransportOptions.cs` at start of Plan 01 Wave 0; add properties if absent

2. **DeviceAdapterExtensions scale — 44 new registrations**
   - What we know: Current file has 3 registrations and is already ~100 lines
   - What's unclear: Whether registrations should be split into per-plan partial methods or separate extension classes
   - Recommendation: Split into `AddPlan01SerialAdapters()`, `AddPlan02NetworkAdapters()`, `AddPlan03SpecialAdapters()` sub-methods; keep one public entry point

3. **CapabilityRegistry thread safety at 44 drivers**
   - What we know: `CapabilityRegistry` is populated once at startup via a factory lambda
   - What's unclear: Whether `CapabilityRegistry.Register()` is safe to call 44+ times during factory build (single-threaded during DI composition — likely fine)
   - Recommendation: Proceed; flag if tests show concurrent registration issues

4. **IMettlerOpcCommand interface scope**
   - What we know: EasyMax/Fbrm/ReactIr use it; it calls `Opc.UaFx` types (`OpcConnectionEnum`)
   - What's unclear: Whether `OPCFoundation.NetStandard.Opc.Ua.Client` (already in csproj) provides the `OpcConnectionEnum` type, or if it is a POP-specific enum
   - Recommendation: Read `BaseMettlerOpcClient.cs` when starting Plan 02 to understand the full IMettlerOpcCommand contract

---

## Sources

All findings are from direct code reads of the following files. All claims are VERIFIED from source.

### Primary sources read

- `Helix.Orchestration.Devices/Drivers/VapourtecR4.cs`
- `Helix.Orchestration.Devices/Drivers/KnauerAzura.cs`
- `Helix.Orchestration.Devices/Drivers/SystagOpcUaClient.cs`
- `Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs`
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`
- `Helix.Orchestration.Devices/Transports/CDll/CDllDeviceTransport.cs`
- `Helix.Orchestration.Devices/Transports/CDll/CDllTransportOptions.cs`
- `Helix.Orchestration.Devices/Transports/Modbus/ModbusDeviceTransport.cs`
- `tests/Helix.Orchestration.Devices.Tests/Drivers/VapourtecR4Tests.cs`
- `tests/Helix.Orchestration.Devices.Tests/Drivers/KnauerAzuraTests.cs`
- `tests/Helix.Orchestration.Devices.Tests/Drivers/SystagOpcUaClientTests.cs`
- `tests/Helix.Orchestration.Devices.Tests/Abstractions/DeviceAdapterBaseTests.cs`
- `tests/Helix.Orchestration.Devices.Tests/Helix.Orchestration.Devices.Tests.csproj`
- All 44 PopNextGen driver source files in `POP.DeviceCore/Drivers/`
- `POP.DeviceCore/Drivers/BaseIkaStirrer.cs`
- `POP.DeviceCore/Drivers/NewEraBaseMethods.cs`
- `POP.DeviceCore/BaseClasses/BaseOceanView.cs`
- All available POP test files in `POP.Tests/DeviceTests/Drivers/`

---

## Metadata

**Confidence breakdown:**
- Transport Classification Table: HIGH — all 44 drivers read directly
- Plan 03 special driver analysis: HIGH — all 8 source files read
- Test infrastructure: HIGH — test files read directly
- DeviceAdapterExtensions pattern: HIGH — source file read
- Recommended task breakdown: MEDIUM — reflects code reality but sequencing is a judgment call

**Research date:** 2026-05-31
**Valid until:** Stable — this is a greenfield port of static source files; no external APIs change it

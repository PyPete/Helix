# Plan 01: Serial Drivers (32 drivers)

**Phase:** driver-port
**Goal:** Port all 32 serial device drivers from POP.DeviceCore into Helix.Orchestration.Devices, with xUnit tests, following the VapourtecR4 pattern.
**Waves:** 5 (Wave 0 prerequisite + Batches A/B/C + Wave 4 DI registration)

---

## Goal

Port 32 serial drivers from PopNextGen (`POP.DeviceCore/Drivers/`) into `Helix.Orchestration.Devices/Drivers/`, each as a `DeviceAdapterBase` subclass using `SerialDeviceTransport`. Port the corresponding POP test for each driver into an xUnit test in `tests/Helix.Orchestration.Devices.Tests/Drivers/`, using the shared `FakeSerial` test helper. Register all 32 drivers in `DeviceAdapterExtensions.AddPlan01SerialAdapters()`.

---

## Pre-conditions

- [ ] `VapourtecR4.cs` exists and compiles â€” confirms the serial driver pattern is established
- [ ] `VapourtecR4Tests.cs` exists â€” source of the `FakeSerialPortConnection` infrastructure to extract
- [ ] `Helix.Orchestration.Devices.Tests.csproj` references NSubstitute and xUnit

---

## Waves

---

### Wave 0: Shared Infrastructure (prerequisite â€” must complete before any batch)
**Parallelizable:** No

#### Task 0.1: Extract shared FakeSerial test helper and verify SerialTransportOptions

**Type:** implement
**Files:**
- Write: `tests/Helix.Orchestration.Devices.Tests/TestHelpers/FakeSerial.cs`
- Read: `tests/Helix.Orchestration.Devices.Tests/Drivers/VapourtecR4Tests.cs` (lines 347â€“421 â€” FakeSerialPortConnectionFactory, FakeSerialPortConnection, FakeSerialStream)
- Read: `Helix.Orchestration.Devices/Transports/Serial/SerialTransportOptions.cs`

**Instructions:**

Create `tests/Helix.Orchestration.Devices.Tests/TestHelpers/FakeSerial.cs` with namespace `Helix.Orchestration.Devices.Tests.TestHelpers`.

Copy the following three classes verbatim from `VapourtecR4Tests.cs` and make them `public` (they are currently `private sealed` nested classes):
- `FakeSerialPortConnectionFactory` (implements `ISerialPortConnectionFactory`)
- `FakeSerialPortConnection` (implements `ISerialPortConnection`)
- `FakeSerialStream` (extends `Stream`)

Required usings: `System.Text`, `Helix.Orchestration.Devices.Transports.Serial`.

Add a public static helper method to assist test setup:

```
public static SerialDeviceTransport MakeTransport(
    FakeSerialPortConnection port,
    string portName = "COM1",
    int baudRate = 9600,
    string commandTerminator = "\r\n",
    System.IO.Ports.Parity parity = System.IO.Ports.Parity.None,
    int dataBits = 8)
```

This method constructs a `SerialDeviceTransport` using `SerialTransportOptions` with the provided values plus `NullLogger<SerialDeviceTransport>.Instance` and a `FakeSerialPortConnectionFactory` wrapping the given port. Required using: `Microsoft.Extensions.Logging.Abstractions`.

Then read `SerialTransportOptions.cs`. If `Parity` and `DataBits` properties are absent, add them:
- `public System.IO.Ports.Parity Parity { get; set; } = System.IO.Ports.Parity.None;`
- `public int DataBits { get; set; } = 8;`

These are required by `IkaOverheadStirrerOldDevice` (Batch B). If they already exist, skip the addition.

Verify `SerialDeviceTransport` constructor passes `Parity` and `DataBits` to `System.IO.Ports.SerialPort`. If not, wire the properties through. (Check `SerialDeviceTransport.cs` to confirm.)

**UAT:**
- [ ] `tests/Helix.Orchestration.Devices.Tests/TestHelpers/FakeSerial.cs` exists and compiles
- [ ] `FakeSerialPortConnection`, `FakeSerialPortConnectionFactory`, `FakeSerialStream` are `public`
- [ ] `MakeTransport` static helper exists and accepts `baudRate`, `commandTerminator`, `parity`, `dataBits`
- [ ] `dotnet build Helix.Orchestration.slnx` passes

---

### Wave 1 â€” Batch A (11 serial drivers)
**Parallelizable:** Yes â€” all tasks are independent (no shared file writes)

Batch A drivers: AalborgDfm, Alicat, ArduinoConductivity, ArduinoLeak, ArduinopH, ArduinoPressure, ArduinoSignalTower, ArduinoTemp, ArduinoValves, Cml18, KernPcb

**Serial settings per driver (from RESEARCH.md):**

| Driver | BaudRate | CmdTerminator | has_trends | Notes |
|--------|----------|---------------|------------|-------|
| AalborgDfm | 9600 | `\r` | YES | |
| Alicat | 19200 | `\r` | YES | BaseMassFlowController |
| ArduinoConductivity | 9600 | (none) | YES | No suffix in Encode |
| ArduinoLeak | 9600 | (none) | YES | Simple sensor |
| ArduinopH | 9600 | (none) | YES | |
| ArduinoPressure | 9600 | (none) | YES | |
| ArduinoSignalTower | 9600 | (none) | NO | BaseMultiValve â€” do NOT implement StreamTrendsAsync |
| ArduinoTemp | 9600 | (none) | YES | |
| ArduinoValves | 9600 | (none) | NO | BaseMultiValve â€” do NOT implement StreamTrendsAsync |
| Cml18 | 9600 | `\r\n` | YES | Simple sensor |
| KernPcb | 9600 | `\r\n` | YES | Balance |

#### Task 1.1: Port AalborgDfm, Alicat, ArduinoConductivity, ArduinoLeak

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/AalborgDfm.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/AalborgDfmTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/Alicat.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/AlicatTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/ArduinoConductivity.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ArduinoConductivityTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/ArduinoLeak.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ArduinoLeakTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\AalborgDfm.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\AalborgDfmTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Alicat.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\AlicatTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ArduinoConductivity.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ArduinoConductivityTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ArduinoLeak.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ArduinoLeakTests.cs`

**Instructions:**

For each driver, apply the serial driver pattern from `PATTERNS.md` and `VapourtecR4.cs`:

**Driver file structure (`Helix.Orchestration.Devices/Drivers/{Name}.cs`):**

```
namespace Helix.Orchestration.Devices.Drivers;

public sealed class {DriverName} : DeviceAdapterBase
{
    public {DriverName}(SerialDeviceTransport transport, ILogger<{DriverName}> logger)
        : base(transport, logger) { }

    private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd);
    private static string Decode(byte[] r) => Encoding.ASCII.GetString(r).Trim();

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder) { ... }

    // Only override StreamTrendsAsync if has_trends=YES
    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken ct = default) { ... }
}
```

Usings: `System.Text`, `System.Text.Json`, `System.Runtime.CompilerServices`, `Helix.Orchestration.Devices.Abstractions`, `Helix.Orchestration.Devices.Transports.Serial`, `Microsoft.Extensions.Logging`.

**Port fidelity rules (apply to every driver):**
1. Read the POP source file. Copy every capability that exists in the POP driver AND is exercised by at least one POP test.
2. Command strings: copy EXACTLY including any `\r`, `\r\n`, spaces. The POP `SendCommand(cmd, timeout)` call argument becomes the string passed to `Encode()`.
3. Response parsing: copy exact logic â€” same `.Split()` separators, same field indices, same string comparisons.
4. Validation: copy range checks and error conditions verbatim.
5. `Info.Success()` â†’ `CapabilityInvocationResult.Successful()`. `Info.Success(value)` â†’ `CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(value))`. `Info.Failure(msg)` â†’ `CapabilityInvocationResult.Failed(msg)`.
6. For `StreamTrendsAsync`: port the POP `GetTrends()` logic. Each POP `Trend<IValue>` with a float/double value maps to a `TrendSample`. Poll every 2 seconds. Wrap each Transport call in try/catch; log warning and continue on failure. Catch `OperationCanceledException` on `Task.Delay` and `yield break`.
7. Do NOT add capabilities that are absent from POP source.

**AalborgDfm specifics:** BaudRate 9600, CmdTerminator `\r` â€” Encode appends `\r`, e.g. `Encode("F\r")` or the terminator is part of the command string in POP source (copy exactly). ~8 capabilities. has_trends YES.

**Alicat specifics:** BaudRate 19200, CmdTerminator `\r`. Inherits from `BaseMassFlowController` in POP â€” the capabilities available are those the POP test exercises (Start, Stop, SetPoint, GetSetPoint, GetSetPointLimits, TareAll, GetTrends). ~6 capabilities. has_trends YES.

**ArduinoConductivity specifics:** BaudRate 9600, no command terminator suffix (commands are sent as-is, no `\r` or `\r\n` appended). ~7 capabilities. has_trends YES.

**ArduinoLeak specifics:** BaudRate 9600, no command terminator suffix. ~4 capabilities. has_trends YES.

**Test file structure (`tests/Helix.Orchestration.Devices.Tests/Drivers/{Name}Tests.cs`):**

```
using Helix.Orchestration.Devices.Tests.TestHelpers;
namespace Helix.Orchestration.Devices.Tests.Drivers;

public sealed class {DriverName}Tests
{
    private static {DriverName} CreateDriver(int baudRate, string terminator, params string[] responses)
    {
        var port = new FakeSerialPortConnection();
        foreach (var r in responses) port.EnqueueRead(r + terminator);
        var transport = FakeSerial.MakeTransport(port, baudRate: baudRate, commandTerminator: terminator);
        var driver = new {DriverName}(transport, NullLogger<{DriverName}>.Instance);
        transport.ConnectAsync().GetAwaiter().GetResult();
        return driver;
    }
    ...
}
```

For each POP test, write a corresponding xUnit `[Fact]`. Naming: `{CapabilityName}_{Condition}_{ExpectedOutcome}`. Assert:
- `Assert.True(result.Success)` / `Assert.False(result.Success)`
- `Assert.Equal(expectedBytes, port.Writes[0])` or `Assert.StartsWith(prefix, Encoding.ASCII.GetString(port.Writes[0]))` to verify command strings
- `Assert.Contains(...)` on `result.ErrorMessage` for failure cases

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~AalborgDfm"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Alicat"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ArduinoConductivity"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ArduinoLeak"` passes
- [ ] All command strings verified to match POP source
- [ ] All capabilities from POP source that have POP test coverage are registered in `RegisterCapabilities`

---

#### Task 1.2: Port ArduinopH, ArduinoPressure, ArduinoSignalTower, ArduinoTemp

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/ArduinopH.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ArduinopHTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/ArduinoPressure.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ArduinoPressureTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/ArduinoSignalTower.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ArduinoSignalTowerTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/ArduinoTemp.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ArduinoTempTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ArduinopH.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ArduinopHTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ArduinoPressure.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ArduinoPressureTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ArduinoSignalTower.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ArduinoSignalTowerTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ArduinoTemp.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ArduinoTempTests.cs`

**Instructions:**

Apply same serial port fidelity rules as Task 1.1.

**ArduinopH specifics:** BaudRate 9600, no command terminator. ~7 capabilities. has_trends YES.

**ArduinoPressure specifics:** BaudRate 9600, no command terminator. ~4 capabilities. has_trends YES.

**ArduinoSignalTower specifics:** BaudRate 9600, no command terminator. ~10 capabilities (from `BaseMultiValve`). has_trends NO â€” do NOT override `StreamTrendsAsync`. Capabilities are GoTo, GetValves, GetNumberOfValves and other valve-control commands present in POP source.

**ArduinoTemp specifics:** BaudRate 9600, no command terminator. ~5 capabilities. has_trends YES.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ArduinopH"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ArduinoPressure"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ArduinoSignalTower"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ArduinoTemp"` passes
- [ ] ArduinoSignalTower does NOT override StreamTrendsAsync

---

#### Task 1.3: Port ArduinoValves, Cml18, KernPcb

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/ArduinoValves.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ArduinoValvesTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/Cml18.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/Cml18Tests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/KernPcb.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/KernPcbTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ArduinoValves.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ArduinoValvesTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Cml18.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\Cml18Tests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\KernPcb.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\KernPcbTests.cs`

**Instructions:**

Apply same serial port fidelity rules as Task 1.1.

**ArduinoValves specifics:** BaudRate 9600, no command terminator. ~10 capabilities (BaseMultiValve). has_trends NO â€” do NOT override StreamTrendsAsync.

**Cml18 specifics:** BaudRate 9600, CmdTerminator `\r\n`. ~3 capabilities. Simple sensor. has_trends YES.

**KernPcb specifics:** BaudRate 9600, CmdTerminator `\r\n`. ~5 capabilities. Balance. has_trends YES.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ArduinoValves"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Cml18"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~KernPcb"` passes

---

### Wave 2 â€” Batch B (11 serial drivers)
**Parallelizable:** Yes â€” tasks are independent. Wave 0 must be complete first (uses FakeSerial helper; IkaOverheadStirrerOldDevice needs Parity/DataBits from Wave 0 fix).

Batch B drivers: AtlasSyringePump, Carino9, ColeParmerTouch, FoxyR1, Gynkotek, Heidolph, IkaDigitalHotPlate, IkaOverheadStirrer, IkaOverheadStirrerOldDevice, IkaUltraTurrax, KnauerP2And4Pump

**Serial settings per driver:**

| Driver | BaudRate | CmdTerminator | has_trends | Notes |
|--------|----------|---------------|------------|-------|
| AtlasSyringePump | varies | (from POP source) | YES | dual-syringe pump, ~18 caps |
| Carino9 | varies | (from POP source) | YES | BaseHighPressurePump |
| ColeParmerTouch | varies | (from POP source) | YES | syringe pump, ~15 caps |
| FoxyR1 | varies | (from POP source) | YES | Trend returns Integer â€” cast to double |
| Gynkotek | varies | (from POP source) | YES | BaseHighPressurePump |
| Heidolph | varies | (from POP source) | YES | BaseStirrerDevice |
| IkaDigitalHotPlate | 9600 | (from POP source) | YES | hotplate stirrer |
| IkaOverheadStirrer | varies | (from POP source) | YES | BaseIkaStirrer |
| IkaOverheadStirrerOldDevice | varies | `\r\n` | YES | 7 DataBits, Even Parity â€” uses Parity/DataBits from Wave 0 |
| IkaUltraTurrax | varies | (from POP source) | YES | BaseIkaStirrer |
| KnauerP2And4Pump | varies | (from POP source) | YES | BaseHighPressurePump |

#### Task 2.1: Port AtlasSyringePump, Carino9, ColeParmerTouch, FoxyR1

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/AtlasSyringePump.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/AtlasSyringePumpTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/Carino9.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/Carino9Tests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/ColeParmerTouch.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ColeParmerTouchTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/FoxyR1.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/FoxyR1Tests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\AtlasSyringePump.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\AtlasSyringePumpTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Carino9.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\Carino9Tests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ColeParmerTouch.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ColeParmerTouchTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\FoxyR1.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\FoxyR1Tests.cs`

**Instructions:**

Apply same serial port fidelity rules as Task 1.1. Read each POP source file first to extract exact BaudRate, command terminator, capabilities, and response parsing.

**FoxyR1 trend quirk:** `GetTrends()` returns `List<Trend<Integer>>` in POP, not `Trend<IValue>`. `Integer` wraps an `int` (tube number). In `StreamTrendsAsync`, cast the value to `(double)intValue`. `TrendSample.Unit` is `null` for state/tube-number values.

**AtlasSyringePump specifics:** Large capability set (~18). Dual syringe pump. Read POP source for exact command strings â€” do not guess. Port all capabilities that have test coverage in `AtlasSyringePumpTests.cs`.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~AtlasSyringePump"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Carino9"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ColeParmerTouch"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~FoxyR1"` passes

---

#### Task 2.2: Port Gynkotek, Heidolph, IkaDigitalHotPlate, IkaOverheadStirrer

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/Gynkotek.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/GynkotekTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/Heidolph.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/HeidolphTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/IkaDigitalHotPlate.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/IkaDigitalHotPlateTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/IkaOverheadStirrer.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/IkaOverheadStirrerTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Gynkotek.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\GynkotekTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Heidolph.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\HeidolphTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\IkaDigitalHotPlate.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\IkaDigitalHotPlateTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\IkaOverheadStirrer.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\IkaOverheadStirrerTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\BaseClasses\BaseIkaStirrer.cs` (for IkaOverheadStirrer capabilities)

**Instructions:**

Apply same serial port fidelity rules as Task 1.1. Read each POP source file to get exact BaudRate, terminator, command strings.

**IkaOverheadStirrer specifics:** Inherits capabilities from `BaseIkaStirrer` in POP. Read `BaseIkaStirrer.cs` to discover the full capability set (Start, Stop, SetPoint, GetSetPoint, GetSetPointLimits, GetTrends â€” torque+speed+setpoint). `IkaOverheadStirrer` may add device-specific overrides. Port all capabilities exercised by `IkaOverheadStirrerTests.cs`.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Gynkotek"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Heidolph"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~IkaDigitalHotPlate"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~IkaOverheadStirrer"` passes

---

#### Task 2.3: Port IkaOverheadStirrerOldDevice, IkaUltraTurrax, KnauerP2And4Pump

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/IkaOverheadStirrerOldDevice.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/IkaOverheadStirrerOldDeviceTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/IkaUltraTurrax.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/IkaUltraTurraxTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/KnauerP2And4Pump.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/KnauerP2And4PumpTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\IkaOverheadStirrerOldDevice.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\IkaOverheadStirrerOldDeviceTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\IkaUltraTurrax.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\IkaUltraTurraxTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\BaseIkaStirrer.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\KnauerP2And4Pump.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\KnauerP2And4PumpTests.cs`

**Instructions:**

Apply same serial port fidelity rules as Task 1.1.

**IkaOverheadStirrerOldDevice specifics:** This driver overrides `Parity = Even` and `DataBits = 7` in POP. In Helix, pass `parity: System.IO.Ports.Parity.Even, dataBits: 7` to `FakeSerial.MakeTransport()` in the test. The DI registration in Wave 4 must also set these on `SerialTransportOptions`. The driver itself is constructed with `SerialDeviceTransport` and the options flow from configuration. Read POP source to confirm exact values. Same capability set as `IkaOverheadStirrer` (inherits BaseIkaStirrer via IkaOverheadStirrer).

**IkaUltraTurrax specifics:** Inherits from `BaseIkaStirrer` directly. Port capabilities exercised by `IkaUltraTurraxTests.cs`. has_trends YES (~7 caps).

**KnauerP2And4Pump specifics:** `BaseHighPressurePump` hierarchy. Port capabilities exercised by `KnauerP2And4PumpTests.cs`. has_trends YES (~8 caps).

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~IkaOverheadStirrerOldDevice"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~IkaUltraTurrax"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~KnauerP2And4Pump"` passes
- [ ] IkaOverheadStirrerOldDevice test uses Parity.Even and DataBits=7 in MakeTransport call

---

### Wave 3 â€” Batch C (11 serial drivers)
**Parallelizable:** Yes â€” tasks are independent.

Batch C drivers: LambdaDoser, MasterFlexLsDigital, MettlerBalance, NewEra1000, OrionVersaStarPro, PilotOne, SartoriusBalance, SuperDew3, Vario, ViciActuator, ViciMultiValveActuator

#### Task 3.1: Port LambdaDoser, MasterFlexLsDigital, MettlerBalance, NewEra1000

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/LambdaDoser.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/LambdaDoserTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/MasterFlexLsDigital.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/MasterFlexLsDigitalTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/MettlerBalance.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/MettlerBalanceTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/NewEra1000.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/NewEra1000Tests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\LambdaDoser.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\LambdaDoserTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\MasterFlexLsDigital.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\MasterFlexLsDigitalTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\MettlerBalance.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\MettlerBalanceTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\NewEra1000.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\NewEra1000Tests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\NewEraBaseMethods.cs`

**Instructions:**

Apply same serial port fidelity rules as Task 1.1.

**NewEra1000 delegation pattern:** In POP, `NewEra1000` creates `new NewEraBaseMethods(SendCommand)` and calls methods on it. `NewEraBaseMethods` is NOT a base class â€” it is a helper class used via delegation. In Helix, do NOT create a `NewEraBaseMethods` class. Instead, read `NewEraBaseMethods.cs` to extract the command strings and logic for each method (`Start`, `Stop`, `Pause`, `SetRate`, `SetVolume`, `SetDiameter`, `Configure`, `ReverseDirection`, `GetTrends`), then implement them directly in the `NewEra1000` Helix driver. Port only the methods that `NewEra1000Tests.cs` exercises.

**LambdaDoser specifics:** `BaseSetPointDevice` hierarchy. Solids doser. ~7 caps. has_trends YES.

**MasterFlexLsDigital specifics:** `BasePeristalticPumpConti` hierarchy. ~15 caps. has_trends YES.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~LambdaDoser"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~MasterFlexLsDigital"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~MettlerBalance"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~NewEra1000"` passes
- [ ] NewEra1000 does not create a NewEraBaseMethods helper class

---

#### Task 3.2: Port OrionVersaStarPro, PilotOne, SartoriusBalance, SuperDew3

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/OrionVersaStarPro.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/OrionVersaStarProTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/PilotOne.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/PilotOneTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/SartoriusBalance.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/SartoriusBalanceTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/SuperDew3.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/SuperDew3Tests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\OrionVersaStarPro.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\OrionVersaStarProTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\PilotOne.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\PilotOneTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\SartoriusBalance.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\SartoriusBalanceTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\SuperDew3.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\SuperDew3Tests.cs`

**Instructions:**

Apply same serial port fidelity rules as Task 1.1.

**OrionVersaStarPro specifics:** Probe/sensor. ~7 caps. has_trends YES. Capability prefix in DI: `orionversastarpro`.

**PilotOne specifics:** `BaseSetPointDevice` hierarchy. Heater/chiller. ~15 caps. has_trends YES.

**SartoriusBalance specifics:** Balance. ~5 caps. has_trends YES.

**SuperDew3 specifics:** Sensor. ~3 caps. has_trends YES.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~OrionVersaStarPro"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~PilotOne"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~SartoriusBalance"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~SuperDew3"` passes

---

#### Task 3.3: Port Vario, ViciActuator, ViciMultiValveActuator

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/Vario.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/VarioTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/ViciActuator.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ViciActuatorTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/ViciMultiValveActuator.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ViciMultiValveActuatorTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Vario.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\VarioTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ViciActuator.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ViciActuatorTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ViciMultiValveActuator.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ViciMultiValveActuatorTests.cs`

**Instructions:**

Apply same serial port fidelity rules as Task 1.1.

**Vario specifics:** `BaseVacPumpDevice` hierarchy. Vacuum pump. ~10 caps. has_trends YES.

**ViciActuator specifics:** Valve actuator. ~9 caps. has_trends YES.

**ViciMultiValveActuator specifics:** `BaseMultiValve` hierarchy. ~9 caps. has_trends NO â€” do NOT override StreamTrendsAsync.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Vario"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ViciActuator"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ViciMultiValveActuator"` passes
- [ ] ViciMultiValveActuator does NOT override StreamTrendsAsync

---

### Wave 4: DI Registration for all 32 serial drivers
**Parallelizable:** No â€” all drivers must be compiled (Waves 0â€“3 complete) before registration

**Depends on:** Waves 0, 1, 2, 3 all complete

#### Task 4.1: Register all 32 serial drivers in DeviceAdapterExtensions

**Type:** implement
**Files:**
- Modify: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`
- Read: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`

**Instructions:**

In `DeviceAdapterExtensions.cs`, add a new private static method `AddPlan01SerialAdapters(IServiceCollection services)` and call it from `AddMigratedDeviceAdapters()`.

For each of the 32 serial drivers, add a service registration block inside `AddPlan01SerialAdapters`. Use the inline constructor pattern (not `services.AddSingleton<SerialTransportOptions>()`) to avoid singleton collisions between drivers sharing the same type:

```
services.AddSingleton(sp => new {DriverName}(
    new SerialDeviceTransport(
        new SerialTransportOptions
        {
            PortName = "{placeholder_port}",
            BaudRate = {baudRate},
            CommandTerminator = "{terminator}",
            Parity = System.IO.Ports.Parity.{parity},  // only for IkaOverheadStirrerOldDevice
            DataBits = {dataBits}                        // only for IkaOverheadStirrerOldDevice
        },
        sp.GetRequiredService<ILogger<SerialDeviceTransport>>()),
    sp.GetRequiredService<ILogger<{DriverName}>>()));
```

Standard placeholder ports: COM4 through COM35 (one per driver, sequential). These are placeholders â€” production overrides via appsettings.

Standard IkaOverheadStirrerOldDevice settings: `Parity = System.IO.Ports.Parity.Even`, `DataBits = 7`.

Then inside the `CapabilityRegistry` builder lambda (after the existing VapourtecR4/KnauerAzura/SystagOpcUaClient blocks), add capability registration for each new driver:

```
var {varName} = sp.GetRequiredService<{DriverName}>();
foreach (var cap in {varName}.GetCapabilities())
{
    var key = $"{prefix}.{cap.Name}";
    registry.Register(key, new HelixAdapterCapabilityBridge({varName}));
    logger.LogInformation("Registered capability: {CapabilityName}", key);
}
```

Capability prefixes (lowercase class name, from RESEARCH.md):

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
| ColeParmerTouch | `coleparmertouch` |
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
| OrionVersaStarPro | `orionversastarpro` |
| PilotOne | `pilotone` |
| SartoriusBalance | `sartoriusbalance` |
| SuperDew3 | `superdew3` |
| Vario | `vario` |
| ViciActuator | `viciactuator` |
| ViciMultiValveActuator | `vicimultivalveactuator` |

**UAT:**
- [ ] `dotnet build Helix.Orchestration.slnx` passes
- [ ] All 32 driver class names appear in `DeviceAdapterExtensions.cs`
- [ ] All 32 capability prefixes from the table above appear in the CapabilityRegistry builder
- [ ] `grep -c "AddSingleton" Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` returns >= 35

---

## Overall UAT

- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/` passes (all tests, zero failures)
- [ ] `dotnet build Helix.Orchestration.slnx` passes
- [ ] All 32 driver `.cs` files exist in `Helix.Orchestration.Devices/Drivers/`
- [ ] All 32 test `.cs` files exist in `tests/Helix.Orchestration.Devices.Tests/Drivers/`
- [ ] `tests/Helix.Orchestration.Devices.Tests/TestHelpers/FakeSerial.cs` exists
- [ ] No capabilities extrapolated beyond POP source + tests
- [ ] Command strings verified by test assertions to match POP source exactly

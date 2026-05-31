# Phase Driver-Port: PopNextGen Driver Port — Context

**Phase:** Driver Port
**Date:** 2026-05-30
**Status:** context captured, ready to plan

---

<domain>
Port 44 device drivers from POP.DeviceCore (PopNextGen) into Helix.Orchestration.Devices,
following the DeviceAdapterBase pattern established by VapourtecR4.cs.
Scope: the 51 drivers with test coverage, minus 3 already done (VapourtecR4, KnauerAzura,
SystagOpcUaClient) and minus 3 native P/Invoke wrappers (Camera, PicoTc08LowLevelApiImports,
XimeaCamera) that cannot be ported without bundled DLLs.
</domain>

<decisions>

## Test Migration Strategy

**Decision: Port the tests too.**

Each PopNextGen test must be translated into a Helix xUnit test that:
- Mocks `IDeviceTransport` (not `IPort`) — Helix uses `SendReceiveAsync(byte[], CancellationToken)`
- Verifies the same serial/TCP command strings the POP test verified
- Asserts `CapabilityInvocationResult.Success` / `.Failed` (not `Info.Success` / `.Failure`)
- Lives in `tests/Helix.Orchestration.Devices.Tests/Drivers/{DriverName}Tests.cs`

The POP tests are the behavioural spec. Every command string, response parse, and edge case
they verify MUST be preserved in the ported Helix test.

## Scope

**Decision: 51 tested drivers, minus 3 already done, minus 3 native-only = 44 drivers.**

### Serial drivers to port (32)
AalborgDfm, Alicat, ArduinoConductivity, ArduinoLeak, ArduinopH, ArduinoPressure,
ArduinoSignalTower, ArduinoTemp, ArduinoValves, AtlasSyringePump, Carino9, Cml18,
ColeParmerTouch, FoxyR1, Gynkotek, Heidolph, IkaDigitalHotPlate, IkaOverheadStirrer,
IkaOverheadStirrerOldDevice, IkaUltraTurrax, KernPcb, KnauerP2And4Pump, LambdaDoser,
MasterFlexLsDigital, MettlerBalance, NewEra1000, OrionVersaStarPro, PilotOne,
SartoriusBalance, SuperDew3, Vario, ViciActuator, ViciMultiValveActuator

### TCP drivers to port (1)
Microsaic (KnauerAzura already done)

### OPC-UA drivers to port (3)
EasyMax, Fbrm, ReactIr

### Other/Special transport drivers to port (8)
AtlasReactionSystem, Crystalline, IfmMasterController, OceanViewDriver, OceanViewNir,
OceanViewUv, PicoTc08, PicoTc08LowLevelWrapper
(transport type to be confirmed during research — may require custom IDeviceTransport impl)

### Excluded (already done)
VapourtecR4, KnauerAzura, SystagOpcUaClient

### Excluded (native DLL — cannot port without bundled binaries)
Camera, PicoTc08LowLevelApiImports, XimeaCamera

## Batching by Transport Type

**Decision: Split into 3 plans by transport type.**

- Plan 01: Serial drivers (32) — batch into sub-groups of ~10 per task
- Plan 02: TCP (1) + OPC-UA (3) = 4 drivers
- Plan 03: Other/Special (8 drivers — research transport before planning)

## Capability Naming Convention

**Decision: Device-prefixed lowercase, matching existing pattern.**

Format: `{devicename}.{CapabilityName}` — e.g., `alicat.SetFlow`, `heidolph.SetSpeed`.
- Device name: lowercase class name (e.g., `VapourtecR4` → `vapourtec`, `AalborgDfm` → `aalborgdfm`)
- Capability name: exact name from the POP source capability method (e.g., `Start`, `Stop`, `SetFlow`)
- Follows existing: `vapourtec.Start`, `knauer.Start`, `systag.GetSystemInfo`
- Registered in `DeviceAdapterExtensions.AddMigratedDeviceAdapters()`

## Port Fidelity Rules

**Decision: Faithful port — no extrapolation.**

1. **Command strings**: copy exactly from POP source, including terminator (`\r\n`, `\r`, etc.)
2. **Response parsing**: copy exact parse logic — same field indices, same split characters
3. **Validation**: copy range checks and error conditions exactly
4. **No new capabilities**: only register capabilities that exist in the POP source AND are verified by tests
5. **Transport mapping**: `IPort.SendCommand(cmd, timeout)` → `Transport.SendReceiveAsync(Encode(cmd), ct)` — synchronous POP API maps to async Helix API with UTF-8 or ASCII encoding matching the POP driver
6. **Result mapping**: `Info.Success()` → `CapabilityInvocationResult.Successful()`, `Info.Failure(msg)` → `CapabilityInvocationResult.Failed(msg)`
7. **Trend mapping**: `GetTrends()` returning `List<Trend<IValue>>` → `StreamTrendsAsync()` returning `IAsyncEnumerable<TrendSample>` with 2-second polling (matching VapourtecR4 pattern)

</decisions>

<canonical_refs>
## Files Downstream Agents Must Read

### Reference Implementation
- `Helix.Orchestration.Devices/Drivers/VapourtecR4.cs` — canonical Helix driver pattern
- `Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs` — base class with RegisterCapabilities
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` — DI registration pattern (capability namespacing)
- `Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs` — CapabilityDescriptor.CreateWithSchemas

### Test Reference
- `tests/Helix.Orchestration.Devices.Tests/Drivers/VapourtecR4Tests.cs` — canonical Helix test pattern (if exists)
- `tests/Helix.Orchestration.Devices.Tests/Drivers/KnauerAzuraTests.cs` — canonical Helix test pattern

### PopNextGen Source (read before porting each driver)
- `C:\Users搶64\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\{DriverName}.cs`
- `C:\Users搶64\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\{DriverName}Tests.cs`

### Transport Abstractions
- `Helix.Orchestration.Devices/Transports/Serial/SerialDeviceTransport.cs`
- `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`
- `Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs`
</canonical_refs>

<code_context>
## Existing Helix Patterns

### DeviceAdapterBase pattern
```csharp
public sealed class VapourtecR4 : DeviceAdapterBase
{
    public VapourtecR4(SerialDeviceTransport transport, ILogger<VapourtecR4> logger)
        : base(transport, logger) { }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        builder.RegisterSimple("Start", "description", async ct => {
            var resp = Decode(await Transport.SendReceiveAsync(Encode("CMD\r\n"), ct));
            return resp.StartsWith("OK") ? CapabilityInvocationResult.Successful()
                                         : CapabilityInvocationResult.Failed(resp);
        });
        builder.Register(CapabilityDescriptor.CreateWithSchemas("SetX", "desc", parameterSchema: "..."),
            async (parameters, _, ct) => { ... });
    }

    // Trend streaming: polls every 2s
    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested) {
            yield return new TrendSample { TrendName = "vapourtec.temperature.A", Value = ..., Unit = "C" };
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }
}
```

### Capability registration in DI (device prefix pattern)
```csharp
// In DeviceAdapterExtensions.cs
var adapter = sp.GetRequiredService<VapourtecR4>();
foreach (var capability in adapter.GetCapabilities())
{
    var key = $"vapourtec.{capability.Name}";
    registry.Register(key, new HelixAdapterCapabilityBridge(adapter));
}
```

### PopNextGen → Helix API mapping
| PopNextGen | Helix |
|---|---|
| `IPort.SendCommand(cmd, timeout)` | `Transport.SendReceiveAsync(Encode(cmd), ct)` |
| `Info.Success()` | `CapabilityInvocationResult.Successful()` |
| `Info.Failure(msg)` | `CapabilityInvocationResult.Failed(msg)` |
| `GetTrends()` | `StreamTrendsAsync()` with 2s poll |
| Mock `IPort` in tests | Mock `IDeviceTransport.SendReceiveAsync` |
</code_context>

<driver_inventory>
## Complete Scope List (44 drivers)

### Plan 01 — Serial (32 drivers)
Batch A (~11): AalborgDfm, Alicat, ArduinoConductivity, ArduinoLeak, ArduinopH, ArduinoPressure, ArduinoSignalTower, ArduinoTemp, ArduinoValves, AtlasSyringePump, Carino9
Batch B (~11): Cml18, ColeParmerTouch, FoxyR1, Gynkotek, Heidolph, IkaDigitalHotPlate, IkaOverheadStirrer, IkaOverheadStirrerOldDevice, IkaUltraTurrax, KernPcb, KnauerP2And4Pump
Batch C (~10): LambdaDoser, MasterFlexLsDigital, MettlerBalance, NewEra1000, OrionVersaStarPro, PilotOne, SartoriusBalance, SuperDew3, Vario, ViciActuator, ViciMultiValveActuator

### Plan 02 — TCP + OPC-UA (4 drivers)
Microsaic (TCP), EasyMax (OPC-UA), Fbrm (OPC-UA), ReactIr (OPC-UA)

### Plan 03 — Other/Special (8 drivers)
AtlasReactionSystem, Crystalline, IfmMasterController, OceanViewDriver, OceanViewNir, OceanViewUv, PicoTc08, PicoTc08LowLevelWrapper
(Transport type TBD during research — planner must inspect each source file before writing tasks)
</driver_inventory>
# Plan 02: TCP + OPC-UA Drivers (4 drivers)

**Phase:** driver-port
**Goal:** Port Microsaic (TCP) and EasyMax, Fbrm, ReactIr (OPC-UA via IMettlerOpcCommand) into Helix.Orchestration.Devices with xUnit tests, then register all 4 in DeviceAdapterExtensions.
**Waves:** 4 (Wave 0 interface declaration + Wave 1 Microsaic + Wave 2 OPC-UA trio + Wave 3 DI registration)

---

## Goal

Port 4 network-connected device drivers from PopNextGen into Helix.Orchestration.Devices. Microsaic uses TCP and follows the KnauerAzura pattern exactly. EasyMax, Fbrm, and ReactIr use OPC-UA via a Mettler-specific command abstraction (`IMettlerOpcCommand`) that must be declared locally, because these drivers do not call raw OPC node reads directly — they call typed Mettler OPC methods. Register all 4 in a new `AddPlan02NetworkAdapters()` method on `DeviceAdapterExtensions`.

---

## Pre-conditions

- [ ] `KnauerAzura.cs` and `KnauerAzuraTests.cs` exist (TCP analog)
- [ ] `SystagOpcUaClient.cs` and `SystagOpcUaClientTests.cs` exist (OPC-UA analog)
- [ ] `Helix.Orchestration.Devices/Interfaces/` directory exists (from RESEARCH.md)

---

## Waves

---

### Wave 0: Declare IMettlerOpcCommand interface (prerequisite)
**Parallelizable:** No

#### Task 0.1: Declare IMettlerOpcCommand in Helix.Orchestration.Devices

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\BaseClasses\BaseMettlerOpcClient.cs`

**Instructions:**

Read `BaseMettlerOpcClient.cs` to discover the full `IMettlerOpcCommand` interface contract. This interface is used by EasyMax, Fbrm, and ReactIr in POP instead of calling `IPort.SendCommand`.

The interface methods are expected to be:
- `Task<T> GetAtNode<T>(string nodeId, CancellationToken ct)` — reads a typed value from an OPC node
- `Task SetAtNode<T>(string nodeId, T value, CancellationToken ct)` — writes a value to an OPC node
- `Task<IEnumerable<string>> GetTrendsAtNode(string nodeId, CancellationToken ct)` — subscribes to trend data at a node
- `Task CreateSession(string endpointUrl, CancellationToken ct)` — connects to OPC-UA server
- Additional methods as found in POP source

Create `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs`:

```
namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Mettler-Toledo OPC-UA command abstraction used by EasyMax, Fbrm, and ReactIr drivers.
/// Declared locally from POP.DeviceCore.BaseMettlerOpcClient contract.
/// </summary>
public interface IMettlerOpcCommand
{
    // Copy exact method signatures from BaseMettlerOpcClient.cs IMettlerOpcCommand interface
}
```

Note on `OpcConnectionEnum`: if `BaseMettlerOpcClient.cs` uses `OpcConnectionEnum` from Opc.UaFx, check whether that type is available in the already-referenced NuGet package `OPCFoundation.NetStandard.Opc.Ua.Client`. If it is a POP-specific enum, declare it locally in the same file as a `public enum OpcConnectionEnum { ... }` with the values found in POP source.

**UAT:**
- [ ] `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs` exists
- [ ] `dotnet build Helix.Orchestration.slnx` passes with new interface
- [ ] All method signatures match what POP source requires

---

### Wave 1: Microsaic (TCP)
**Parallelizable:** No (single driver, fills a full task)

#### Task 1.1: Port Microsaic

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/Microsaic.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/MicrosaicTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Microsaic.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\MicrosaicTests.cs`
- Read: `Helix.Orchestration.Devices/Drivers/KnauerAzura.cs` (TCP analog)
- Read: `tests/Helix.Orchestration.Devices.Tests/Drivers/KnauerAzuraTests.cs` (test analog)

**Instructions:**

Microsaic is the largest driver in scope (~1000 lines in POP, large capability set with 20+ commands). Read the POP source file completely before writing any code.

**Driver pattern (`Helix.Orchestration.Devices/Drivers/Microsaic.cs`):**

Follow `KnauerAzura.cs` pattern exactly:
- Dual constructors: `public Microsaic(TcpDeviceTransport transport, ILogger<Microsaic> logger)` and `internal Microsaic(IDeviceTransport transport, ILogger<Microsaic> logger)`
- Encoding: ASCII bytes. Terminator: copy from POP source (check `BaseTcpDevice.sendAsync` to identify terminator; likely `\n` or `\r`)
- `Encode(string cmd)` appends the terminator that POP uses (copy exactly)
- `Decode(byte[] r)` returns `Encoding.ASCII.GetString(r).Trim()`

**Async factory `Create()` in POP:** Microsaic uses `public static async Task<Microsaic> Create(...)`. In Helix, drop the static factory — use a regular constructor. The constructor-time initialization that POP does in `Create()` (e.g., connecting, querying firmware) should be deferred to the first capability call or a `ConnectAsync` capability. Do NOT make the Helix constructor async. If `Create()` fetches initialization data that is then used in capability handlers, store it as a cached field populated lazily on first use.

**Capabilities:** Port only capabilities that `MicrosaicTests.cs` exercises. Do not port capabilities untested in POP. Port fidelity rules: command strings exact, response parsing exact, result mapping as per locked decisions.

**`StreamTrendsAsync`:** Microsaic has `GetTrends()` returning Matrix trends (from research: has_trends=YES, Matrix). `Matrix` wraps a 2D double array (spectrum). Since `TrendSample.Value` is `object` (not strictly `double`), serialize the matrix data as a JSON array in the `Value` field. Check `MicrosaicTests.cs` to understand what the trend data structure looks like; serialize it with `JsonSerializer.SerializeToElement(matrixData)` and assign `Value = JsonSerializer.Serialize(matrixData)`. If the trend is truly a scalar (intensity peak), yield a scalar `TrendSample`. Poll every 2 seconds. Log and continue on transport error.

**Test pattern (`tests/.../Drivers/MicrosaicTests.cs`):**

Follow `KnauerAzuraTests.cs` pattern:
- `CreateDriver(params string[] responses)` returning `(Microsaic driver, IDeviceTransport transport)`
- `SentCommand(IDeviceTransport transport, int callIndex = 0)` helper to inspect sent bytes
- `Json(string json)` helper
- NSubstitute: `Substitute.For<IDeviceTransport>()`; queue ASCII-encoded response bytes
- Use `internal` constructor: `new Microsaic(transport, NullLogger<Microsaic>.Instance)`
- One `[Fact]` per POP test, preserving the same assertions about command strings and results

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Microsaic"` passes
- [ ] All capabilities from `MicrosaicTests.cs` have corresponding Helix tests
- [ ] Command strings verified by test assertions to match POP source
- [ ] `Microsaic.cs` has both public `TcpDeviceTransport` constructor and internal `IDeviceTransport` constructor
- [ ] `dotnet build Helix.Orchestration.slnx` passes

---

### Wave 2: EasyMax, Fbrm, ReactIr (OPC-UA)
**Parallelizable:** Yes — three tasks can run in parallel (no shared file writes beyond IMettlerOpcCommand which is already declared)

**Depends on:** Wave 0 complete (IMettlerOpcCommand declared)

#### Task 2.1: Port EasyMax

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/EasyMax.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/EasyMaxTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\EasyMax.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\EasyMaxTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\BaseClasses\BaseMettlerOpcClient.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs`

**Instructions:**

**EasyMax specifics from RESEARCH.md:** OpcName = `"iControl"`. ~6 capabilities. has_trends YES. Uses `BaseMettlerOpcClient` → `IMettlerOpcCommand` abstraction.

**Driver structure — facade pattern with dual dependencies:**

```
public sealed class EasyMax : DeviceAdapterBase
{
    private readonly IMettlerOpcCommand _opc;

    // Production constructor
    public EasyMax(OpcDeviceTransport transport, IMettlerOpcCommand opc, ILogger<EasyMax> logger)
        : base(transport, logger) { _opc = opc; }

    // Internal constructor for testing (stub transport)
    internal EasyMax(IDeviceTransport transport, IMettlerOpcCommand opc, ILogger<EasyMax> logger)
        : base(transport, logger) { _opc = opc; }
}
```

ALL capability logic goes through `_opc.{method}(...)` — NOT through `Transport.SendReceiveAsync`. The `IDeviceTransport` is a stub (present only because `DeviceAdapterBase` requires it). Do NOT call `Transport.SendReceiveAsync` in any capability handler.

**Port fidelity:** Read `EasyMax.cs` POP source. Copy every capability that `EasyMaxTests.cs` exercises. Copy exact OPC node IDs from POP source (these are string literals like `"iControl/Program/SetpointTemperature"` — copy exactly). Copy exact response parsing. Result mapping as per locked decisions.

**StreamTrendsAsync:** EasyMax has `GetTrends()` in POP via `BaseMettlerOpcClient`. Port as `StreamTrendsAsync` with 2-second poll using `_opc.GetTrendsAtNode(...)`. Yield `TrendSample` per trend value. Trend names use `easymax.{measurementType}` prefix.

**Test pattern:**

```
private static (EasyMax driver, IMettlerOpcCommand opc) CreateDriver()
{
    var transport = Substitute.For<IDeviceTransport>();
    var opc = Substitute.For<IMettlerOpcCommand>();
    // Configure opc substitutes based on EasyMaxTests.cs POP test expectations
    var driver = new EasyMax(transport, opc, NullLogger<EasyMax>.Instance);
    return (driver, opc);
}
```

POP `EasyMaxTests.cs` mocks `IMettlerOpcCommand` — translate these mock setups to NSubstitute syntax. Verify `opc.Received()` calls with correct node IDs (equivalent to POP test assertions).

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~EasyMax"` passes
- [ ] All OPC node IDs match POP source exactly
- [ ] `EasyMax.cs` does NOT call `Transport.SendReceiveAsync`

---

#### Task 2.2: Port Fbrm

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/Fbrm.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/FbrmTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Fbrm.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\FbrmTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\BaseClasses\BaseMettlerOpcClient.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs`

**Instructions:**

**Fbrm specifics from RESEARCH.md:** OpcName = `"iCFBRM"`. ~4 capabilities. has_trends NO (commented out in POP source) — do NOT implement `StreamTrendsAsync`. Uses `BaseMettlerOpcClient` → `IMettlerOpcCommand` abstraction.

Apply same dual-constructor facade pattern as EasyMax (Task 2.1). All capability logic through `_opc.{method}(...)`. Do NOT call `Transport.SendReceiveAsync`.

Read `Fbrm.cs` POP source. Copy every capability that `FbrmTests.cs` exercises. Copy exact OPC node IDs. Because `has_trends=NO`, do not override `StreamTrendsAsync` — the base class empty implementation is correct.

Test pattern: same NSubstitute approach as EasyMax. Mock `IMettlerOpcCommand`; verify node IDs.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Fbrm"` passes
- [ ] Fbrm does NOT override StreamTrendsAsync
- [ ] All OPC node IDs match POP source exactly

---

#### Task 2.3: Port ReactIr

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/ReactIr.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/ReactIrTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\ReactIr.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\ReactIrTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\BaseClasses\BaseMettlerOpcClient.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs`

**Instructions:**

**ReactIr specifics from RESEARCH.md:** OpcName = `"iCIR"`. ~8 capabilities. has_trends NO (no override in POP source) — do NOT implement `StreamTrendsAsync`. Uses `BaseMettlerOpcClient` → `IMettlerOpcCommand` abstraction.

Apply same dual-constructor facade pattern as EasyMax (Task 2.1). All capability logic through `_opc.{method}(...)`. Do NOT call `Transport.SendReceiveAsync`.

Read `ReactIr.cs` POP source. Copy every capability that `ReactIrTests.cs` exercises. Copy exact OPC node IDs (OpcName = `"iCIR"`).

Test pattern: same NSubstitute approach as EasyMax. Mock `IMettlerOpcCommand`; verify node IDs.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~ReactIr"` passes
- [ ] ReactIr does NOT override StreamTrendsAsync
- [ ] All OPC node IDs match POP source exactly

---

### Wave 3: DI Registration for all 4 network drivers
**Parallelizable:** No — all drivers must compile first (Waves 0–2 complete)

**Depends on:** Waves 0, 1, 2 all complete

#### Task 3.1: Register Microsaic and EasyMax/Fbrm/ReactIr in DeviceAdapterExtensions

**Type:** implement
**Files:**
- Modify: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`
- Read: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`

**Instructions:**

Add a new private static method `AddPlan02NetworkAdapters(IServiceCollection services)` and call it from `AddMigratedDeviceAdapters()`.

**Microsaic (TCP) registration:**

```csharp
services.AddSingleton(sp => new TcpDeviceTransport(
    new TcpTransportOptions
    {
        Host = "192.168.1.102",   // placeholder — override via appsettings
        Port = 10001,             // confirm from POP source; adjust if different
        FramingMode = TcpFramingMode.Delimiter,
        DelimiterByte = 0x0A      // '\n'; confirm from POP source
    },
    sp.GetRequiredService<ILogger<TcpDeviceTransport>>()));
services.AddSingleton(sp => new Microsaic(
    sp.GetRequiredService<TcpDeviceTransport>(),
    sp.GetRequiredService<ILogger<Microsaic>>()));
```

Note: Microsaic has its own `TcpDeviceTransport` singleton separate from KnauerAzura (use `sp => new TcpDeviceTransport(...)` factory pattern to avoid sharing the KnauerAzura transport singleton).

**EasyMax, Fbrm, ReactIr (OPC-UA + IMettlerOpcCommand) registration:**

Each OPC-UA driver requires both an `OpcDeviceTransport` and an `IMettlerOpcCommand`. Since `IMettlerOpcCommand` is an interface whose production implementation depends on the OPC client library, register a placeholder implementation that throws `NotImplementedException` for now:

```csharp
// IMettlerOpcCommand production implementation — replace with real impl when available
services.AddSingleton<IMettlerOpcCommand>(sp =>
    throw new NotImplementedException(
        "IMettlerOpcCommand production implementation not yet registered. " +
        "Register a concrete OPC client adapter before starting the application."));
```

Then register each OPC-UA driver (sharing a single `OpcDeviceTransport` per device or separate transports — use separate instances to allow different endpoint URLs):

```csharp
// EasyMax — opc.tcp://easymax-host:4840 (placeholder)
services.AddSingleton(sp => new EasyMax(
    new OpcDeviceTransport(
        new OpcTransportOptions { EndpointUrl = "opc.tcp://localhost:4841", AutoAcceptUntrustedCertificates = false },
        sp.GetRequiredService<ILogger<OpcDeviceTransport>>()),
    sp.GetRequiredService<IMettlerOpcCommand>(),
    sp.GetRequiredService<ILogger<EasyMax>>()));

// Fbrm — opc.tcp://fbrm-host:4840 (placeholder)
services.AddSingleton(sp => new Fbrm(
    new OpcDeviceTransport(
        new OpcTransportOptions { EndpointUrl = "opc.tcp://localhost:4842", AutoAcceptUntrustedCertificates = false },
        sp.GetRequiredService<ILogger<OpcDeviceTransport>>()),
    sp.GetRequiredService<IMettlerOpcCommand>(),
    sp.GetRequiredService<ILogger<Fbrm>>()));

// ReactIr — opc.tcp://reactir-host:4840 (placeholder)
services.AddSingleton(sp => new ReactIr(
    new OpcDeviceTransport(
        new OpcTransportOptions { EndpointUrl = "opc.tcp://localhost:4843", AutoAcceptUntrustedCertificates = false },
        sp.GetRequiredService<ILogger<OpcDeviceTransport>>()),
    sp.GetRequiredService<IMettlerOpcCommand>(),
    sp.GetRequiredService<ILogger<ReactIr>>()));
```

Then inside the `CapabilityRegistry` builder, add capability registration for each new driver:

```csharp
// Microsaic
var microsaic = sp.GetRequiredService<Microsaic>();
foreach (var cap in microsaic.GetCapabilities())
{
    registry.Register($"microsaic.{cap.Name}", new HelixAdapterCapabilityBridge(microsaic));
    logger.LogInformation("Registered capability: {CapabilityName}", $"microsaic.{cap.Name}");
}

// EasyMax
var easymax = sp.GetRequiredService<EasyMax>();
foreach (var cap in easymax.GetCapabilities())
{
    registry.Register($"easymax.{cap.Name}", new HelixAdapterCapabilityBridge(easymax));
    logger.LogInformation("Registered capability: {CapabilityName}", $"easymax.{cap.Name}");
}

// Fbrm
var fbrm = sp.GetRequiredService<Fbrm>();
foreach (var cap in fbrm.GetCapabilities())
{
    registry.Register($"fbrm.{cap.Name}", new HelixAdapterCapabilityBridge(fbrm));
    logger.LogInformation("Registered capability: {CapabilityName}", $"fbrm.{cap.Name}");
}

// ReactIr
var reactir = sp.GetRequiredService<ReactIr>();
foreach (var cap in reactir.GetCapabilities())
{
    registry.Register($"reactir.{cap.Name}", new HelixAdapterCapabilityBridge(reactir));
    logger.LogInformation("Registered capability: {CapabilityName}", $"reactir.{cap.Name}");
}
```

Required additional using: `using Helix.Orchestration.Devices.Interfaces;` (for `IMettlerOpcCommand`).

**UAT:**
- [ ] `dotnet build Helix.Orchestration.slnx` passes
- [ ] `DeviceAdapterExtensions.cs` contains `AddPlan02NetworkAdapters` method call
- [ ] `Microsaic`, `EasyMax`, `Fbrm`, `ReactIr` all appear in registration code
- [ ] Capability prefixes `microsaic.`, `easymax.`, `fbrm.`, `reactir.` all appear in CapabilityRegistry builder

---

## Overall UAT

- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Microsaic|FullyQualifiedName~EasyMax|FullyQualifiedName~Fbrm|FullyQualifiedName~ReactIr"` passes (zero failures)
- [ ] `dotnet build Helix.Orchestration.slnx` passes
- [ ] `Helix.Orchestration.Devices/Interfaces/IMettlerOpcCommand.cs` exists
- [ ] `Helix.Orchestration.Devices/Drivers/Microsaic.cs` exists — TCP driver with dual constructors
- [ ] `Helix.Orchestration.Devices/Drivers/EasyMax.cs`, `Fbrm.cs`, `ReactIr.cs` exist — OPC-UA drivers using `_opc` field
- [ ] No capabilities extrapolated beyond POP source + tests
- [ ] OPC node IDs verified to match POP source exactly

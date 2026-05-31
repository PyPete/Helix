# Plan 03: Special/Custom Transport Drivers (8 drivers)

**Phase:** driver-port
**Goal:** Port 8 drivers that use non-standard transports (File I/O, SQL, IO-Link HTTP, CDll/P-Invoke) into Helix.Orchestration.Devices as facade DeviceAdapterBase subclasses, declare required domain interfaces, and register all in DeviceAdapterExtensions.
**Waves:** 4 (Wave 0 interface declarations + Wave 1 file/SQL facades + Wave 2 CDll + IO-Link adapters + Wave 3 DI registration)

---

## Goal

Eight PopNextGen drivers have no standard serial/TCP/OPC-UA transport. They use File I/O (AtlasReactionSystem), SQL Server (Crystalline), IO-Link HTTP (IfmMasterController), or vendor C DLLs (OceanViewUv, OceanViewNir, OceanViewDriver, PicoTc08, PicoTc08LowLevelWrapper). The porting strategy is a facade pattern: each `DeviceAdapterBase` subclass accepts the domain interface as a constructor parameter plus a stub `IDeviceTransport` required by the base class. All capability logic goes through the domain interface — `Transport.SendReceiveAsync` is never called. Two drivers (OceanViewDriver and PicoTc08LowLevelWrapper) are vendor DLL wrappers that are NOT DeviceAdapterBase subclasses — they implement the domain interface (`IOceanViewDriver` and `IPicoApi` respectively) and are ported as-is.

---

## Pre-conditions

- [ ] `Helix.Orchestration.Devices/Interfaces/` directory exists
- [ ] `DeviceAdapterBase.cs` accepts `IDeviceTransport` (not just concrete transport types) — confirmed for base class
- [ ] `Helix.Orchestration.Devices.Tests.csproj` references NSubstitute

---

## Waves

---

### Wave 0: Declare all required domain interfaces (prerequisite — must complete before all waves)
**Parallelizable:** No

#### Task 0.1: Declare domain interfaces for all Plan 03 drivers

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Interfaces/IOceanViewDriver.cs`
- Write: `Helix.Orchestration.Devices/Interfaces/IPicoApi.cs`
- Write: `Helix.Orchestration.Devices/Interfaces/ICrystallineSqlClient.cs`
- Write: `Helix.Orchestration.Devices/Interfaces/IPopIoLinkClient.cs`
- Write: `Helix.Orchestration.Devices/Interfaces/IDirectoryInfo.cs`
- Write: `Helix.Orchestration.Devices/Interfaces/IReadCsvFile.cs`
- Write: `Helix.Orchestration.Devices/Interfaces/IGetProcess.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\OceanViewDriver.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\OceanViewUv.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\PicoTc08.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\PicoTc08LowLevelWrapper.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Crystalline.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\IfmMasterController.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\AtlasReactionSystem.cs`

**Instructions:**

Read all POP source files to extract the exact method signatures for each interface. Declare each interface in `Helix.Orchestration.Devices/Interfaces/` with `namespace Helix.Orchestration.Devices.Interfaces;`.

**IOceanViewDriver (~12 methods):**
Source: `OceanViewDriver.cs` implements this interface (or `BaseOceanView` uses it). Read `OceanViewDriver.cs` to find the interface declaration or the method set. Expected methods include: `OpenDevice(int id)`, `CloseDevice(int id)`, `SetIntegrationTime(int id, double ms)`, `SetScansToAverage(int id, int scans)`, `SetBoxcarWidth(int id, int width)`, `GetSpectrum(int id)` (returns double[]), `GetWavelengths(int id)` (returns double[]), plus methods needed by tests. Copy exact signatures from POP source.

**IPicoApi (5 methods):**
Source: `PicoTc08.cs` uses `IPicoApi`; `PicoTc08LowLevelWrapper.cs` implements it. Read both. Expected methods: `OpenUnit()` → returns `short` (handle), `CloseUnit(short handle)`, `SetChannel(short handle, int channel, char tcType, char units)`, `GetTemperatures(short handle, float[] temp, ref uint timeMs)`, `GetLastError(short handle)`. Copy exact signatures and return types from POP source.

**ICrystallineSqlClient (~6 methods):**
Source: `Crystalline.cs` uses `ICrystallineSqlClient`. Read to find all calls: `CreateSession(endPoint)`, `CreateSubscription(reactor)`, `Close()`, `FindActiveSystems()`, `GetRuntimeDataForReactor(reactor)`, plus any additional methods. Copy exact signatures from POP source.

**IPopIoLinkClient (~4 methods):**
Source: `IfmMasterController.cs` uses `IPopIoLinkClient`. Read to find all calls: `GetAllProcessDataForDeviceAsync()`, `OpenAsync(endPoint, portNumber)`, `CloseAsync()`, plus others. Copy exact signatures including `Task<T>` return types for async methods.

**IDirectoryInfo (~2-3 methods):**
Source: `AtlasReactionSystem.cs`. Used to list files in a directory. Read to find exact calls: likely `GetFiles(string path)` or `GetDirectoryInfo(string path)` plus file enumeration. Copy exact method signatures.

**IReadCsvFile (~2-3 methods):**
Source: `AtlasReactionSystem.cs`. Used to read CSV content. Read to find exact calls. Copy exact method signatures.

**IGetProcess (~2 methods):**
Source: `AtlasReactionSystem.cs`. Used to check if Atlas desktop process is running. Read to find exact calls: likely `GetProcessesByName(string name)` returning process info. Copy exact signatures.

All interfaces use `namespace Helix.Orchestration.Devices.Interfaces;`. Keep doccomments minimal — just a one-line summary per interface noting it is a local copy of the POP interface.

**UAT:**
- [ ] All 7 interface files exist in `Helix.Orchestration.Devices/Interfaces/`
- [ ] `dotnet build Helix.Orchestration.slnx` passes
- [ ] Every method signature matches what POP source requires (no missing methods that driver code calls)

---

### Wave 1: File I/O and SQL facade drivers
**Parallelizable:** Yes — AtlasReactionSystem and Crystalline are independent

**Depends on:** Wave 0 complete

#### Task 1.1: Port AtlasReactionSystem

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/AtlasReactionSystem.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/AtlasReactionSystemTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\AtlasReactionSystem.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\AtlasReactionSystemTests.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/IDirectoryInfo.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/IReadCsvFile.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/IGetProcess.cs`

**Instructions:**

**AtlasReactionSystem specifics from RESEARCH.md:** File I/O transport. Constructor takes `IDirectoryInfo`, `IReadCsvFile`, `IGetProcess`, `ILogger`. ~4 capabilities. has_trends YES. No `Transport.SendReceiveAsync` calls.

**Driver structure — facade pattern:**

```csharp
using Helix.Orchestration.Devices.Interfaces;

namespace Helix.Orchestration.Devices.Drivers;

public sealed class AtlasReactionSystem : DeviceAdapterBase
{
    private readonly IDirectoryInfo _directoryInfo;
    private readonly IReadCsvFile _csvReader;
    private readonly IGetProcess _processChecker;

    public AtlasReactionSystem(
        IDirectoryInfo directoryInfo,
        IReadCsvFile csvReader,
        IGetProcess processChecker,
        IDeviceTransport transport,   // stub; never used for I/O
        ILogger<AtlasReactionSystem> logger)
        : base(transport, logger)
    {
        _directoryInfo = directoryInfo;
        _csvReader = csvReader;
        _processChecker = processChecker;
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder) { ... }
    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(...) { ... }
}
```

**Port fidelity:** Read `AtlasReactionSystem.cs` POP source. Copy every capability that `AtlasReactionSystemTests.cs` exercises. Capabilities likely include: `Open(path)` (sets the directory path to monitor), `IsDevice()` or equivalent (checks if Atlas process is running via `_processChecker`), plus others. Copy exact logic.

**StreamTrendsAsync:** Port `GetTrends()` logic. In POP, `GetTrends()` reads the most-recently-written CSV in the monitored directory, then reads the latest row. In `StreamTrendsAsync`, poll every 2 seconds: call `_directoryInfo` to find the latest file, call `_csvReader` to read it, parse the latest data row, yield `TrendSample` per measurement. Use `atlasreactionsystem.{measurementType}` trend name prefix. Wrap each poll in try/catch; log warning and continue on error.

**Test pattern:** NSubstitute mocks for `IDirectoryInfo`, `IReadCsvFile`, `IGetProcess`, and `IDeviceTransport`. Use `internal` constructor pattern (add an `internal` constructor for testing or use the public constructor with all mocked dependencies). Match `AtlasReactionSystemTests.cs` POP test assertions.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~AtlasReactionSystem"` passes
- [ ] `AtlasReactionSystem.cs` does NOT call `Transport.SendReceiveAsync`
- [ ] All capabilities from `AtlasReactionSystemTests.cs` have corresponding Helix tests

---

#### Task 1.2: Port Crystalline

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/Crystalline.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/CrystallineTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\Crystalline.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\CrystallineTests.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/ICrystallineSqlClient.cs`

**Instructions:**

**Crystalline specifics from RESEARCH.md:** SQL Server transport via `ICrystallineSqlClient`. Constructor takes `ICrystallineSqlClient`, `ILogger`. ~5 capabilities. has_trends YES. No `Transport.SendReceiveAsync` calls.

**Driver structure — facade pattern:**

```csharp
using Helix.Orchestration.Devices.Interfaces;

namespace Helix.Orchestration.Devices.Drivers;

public sealed class Crystalline : DeviceAdapterBase
{
    private readonly ICrystallineSqlClient _sqlClient;

    public Crystalline(
        ICrystallineSqlClient sqlClient,
        IDeviceTransport transport,   // stub; never used for I/O
        ILogger<Crystalline> logger)
        : base(transport, logger)
    {
        _sqlClient = sqlClient;
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder) { ... }
    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(...) { ... }
}
```

**Port fidelity:** Read `Crystalline.cs` POP source. Copy every capability that `CrystallineTests.cs` exercises. Capabilities: `CreateSession(endPoint)`, `CreateSubscription(reactor)`, `Close()`, `FindActiveSystems()`, plus others. Copy exact logic.

**StreamTrendsAsync and image trends:** POP `GetTrends()` returns trends including a camera image blob. The image trend does NOT fit `TrendSample.Value: object` cleanly (it is a large binary). Skip image trends in `StreamTrendsAsync`. Add a comment: `// Image trends omitted from StreamTrendsAsync — images are not suitable as scalar TrendSample values.` Port non-image scalar trends (temperature readings, reactor data) as `TrendSample` objects. Use `crystalline.{measurementType}` trend name prefix. Poll every 2 seconds.

**Test pattern:** NSubstitute mock for `ICrystallineSqlClient` and `IDeviceTransport`. Match `CrystallineTests.cs` assertions.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~Crystalline"` passes
- [ ] `Crystalline.cs` does NOT call `Transport.SendReceiveAsync`
- [ ] Image trends omitted with comment in `StreamTrendsAsync`

---

### Wave 2: CDll and IO-Link drivers
**Parallelizable:** Yes — PicoTc08 group, OceanView group, and IfmMasterController are independent

**Depends on:** Wave 0 complete

#### Task 2.1: Port OceanViewDriver (CDll wrapper) + OceanViewUv + OceanViewNir

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/OceanViewDriver.cs`
- Write: `Helix.Orchestration.Devices/Drivers/OceanViewUv.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/OceanViewUvTests.cs`
- Write: `Helix.Orchestration.Devices/Drivers/OceanViewNir.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/OceanViewNirTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\OceanViewDriver.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\OceanViewUv.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\OceanViewNir.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\BaseClasses\BaseOceanView.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\OceanViewTests.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/IOceanViewDriver.cs`

**Instructions:**

**OceanViewDriver (NOT a DeviceAdapterBase):**

`OceanViewDriver` is the CDll layer — it wraps `NetOceanDirect.OceanDirect` and `OpticalBench` via P/Invoke or vendor SDK calls. It implements `IOceanViewDriver`. In Helix, port it as a plain class that still implements `IOceanViewDriver`, NOT as a `DeviceAdapterBase`. Copy the P/Invoke declarations, DLL imports, and method implementations from POP source exactly. This class stays as-is — it is the vendor integration layer.

```csharp
namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// CDll wrapper implementing IOceanViewDriver. Wraps the NetOceanDirect vendor SDK.
/// This is NOT a DeviceAdapterBase — it is the native integration layer.
/// </summary>
public sealed class OceanViewDriver : IOceanViewDriver
{
    // Copy P/Invoke / vendor SDK code from POP OceanViewDriver.cs verbatim
}
```

If the vendor SDK (NetOceanDirect) is not referenced in the Helix project, add a `// TODO: Add NetOceanDirect NuGet reference` comment at the top and stub the method bodies with `throw new NotImplementedException("NetOceanDirect vendor DLL not yet referenced");` to keep the class compilable. Do NOT invent P/Invoke signatures — copy exactly from POP source or stub if DLL unavailable.

**OceanViewUv and OceanViewNir (DeviceAdapterBase — facade pattern):**

Both wrap `IOceanViewDriver`. Apply facade pattern:

```csharp
public sealed class OceanViewUv : DeviceAdapterBase
{
    private readonly IOceanViewDriver _driver;

    public OceanViewUv(IOceanViewDriver driver, IDeviceTransport transport, ILogger<OceanViewUv> logger)
        : base(transport, logger) { _driver = driver; }

    // Internal for testing
    internal OceanViewUv(IOceanViewDriver driver, IDeviceTransport transport, ILogger<OceanViewUv> logger)
        : base(transport, logger) { _driver = driver; }  // same as public — can be one constructor
}
```

**Capabilities:** Read `BaseOceanView.cs` to find the full capability set (~13 methods: Open, Close, SetIntegrationTime, SetScansToAverage, SetBoxcarWidth, GetSpectrum, GetWavelengths, etc.). Read `OceanViewTests.cs` (POP) to identify which capabilities are tested. Port only tested capabilities.

Spectrum data returned from `GetSpectrum` is `double[]`. Serialize as: `CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(spectrumArray))`.

**StreamTrendsAsync for OceanViewUv and OceanViewNir:** RESEARCH.md notes that `BaseOceanView.GetTrends()` returns `Trend<Matrix>` where Matrix is a 2D spectrum — NOT a scalar double. Do NOT override `StreamTrendsAsync` for either driver. The default empty base-class implementation is correct. Add a comment: `// StreamTrendsAsync not implemented — spectrum data is non-scalar (Matrix); use GetSpectrum capability instead.`

**Tests:** NSubstitute mock for `IOceanViewDriver` and `IDeviceTransport`. `OceanViewUvTests.cs` and `OceanViewNirTests.cs` mirror the POP `OceanViewTests.cs` assertions (translated to Helix patterns).

Note: there is NO test file for `OceanViewDriver.cs` itself (it is a vendor wrapper). The POP `OceanViewTests.cs` tests the UV/NIR adapters, not the CDll wrapper.

**UAT:**
- [ ] `dotnet build Helix.Orchestration.slnx` passes (OceanViewDriver.cs compiles even if methods are stubbed)
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~OceanViewUv"` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~OceanViewNir"` passes
- [ ] OceanViewUv and OceanViewNir do NOT override StreamTrendsAsync
- [ ] OceanViewDriver does NOT extend DeviceAdapterBase

---

#### Task 2.2: Port PicoTc08LowLevelWrapper (IPicoApi impl) + PicoTc08

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/PicoTc08LowLevelWrapper.cs`
- Write: `Helix.Orchestration.Devices/Drivers/PicoTc08.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/PicoTc08Tests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\PicoTc08LowLevelWrapper.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\PicoTc08.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\PicoTc08Tests.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/IPicoApi.cs`

**Instructions:**

**PicoTc08LowLevelWrapper (NOT a DeviceAdapterBase — IPicoApi implementation):**

This class implements `IPicoApi` by calling `PicoTc08LowLevelApiImports` (P/Invoke). It IS the native layer. Port it as a plain class implementing `IPicoApi`:

```csharp
namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// IPicoApi implementation wrapping PicoTc08LowLevelApiImports (P/Invoke).
/// This is NOT a DeviceAdapterBase — it is the native integration layer for usbtc08.dll.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class PicoTc08LowLevelWrapper : IPicoApi
{
    // Copy P/Invoke declarations and method implementations from POP source verbatim
}
```

Copy all `[DllImport("usbtc08.dll", ...)]` declarations and method implementations exactly from `PicoTc08LowLevelWrapper.cs`. Do NOT change any P/Invoke signatures. If `usbtc08.dll` is unavailable in Helix project, add a comment `// TODO: Add usbtc08.dll to native binaries` but keep all code as-is for compilation.

**PicoTc08 (DeviceAdapterBase — facade wrapping IPicoApi):**

```csharp
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class PicoTc08 : DeviceAdapterBase
{
    private readonly IPicoApi _pico;

    public PicoTc08(IPicoApi pico, IDeviceTransport transport, ILogger<PicoTc08> logger)
        : base(transport, logger) { _pico = pico; }
}
```

**Capabilities:** Read `PicoTc08.cs` POP source. Port only capabilities tested in `PicoTc08Tests.cs`. ~7 capabilities (open unit, close, set channel, get temperatures, etc.).

**StreamTrendsAsync:** RESEARCH.md: has_trends YES — `GetTrends()` polls 8 thermocouple channels. In `StreamTrendsAsync`, poll all 8 channels every 2 seconds: call `_pico.GetTemperatures(handle, temp, ref timeMs)`, then for each channel that returned a valid reading, yield:
```
new TrendSample { TrendName = $"picotc08.temperature.ch{channel}", Value = (double)temp[channel], Timestamp = DateTimeOffset.UtcNow, Unit = "C" }
```
Wrap in try/catch; log warning and continue. Catch `OperationCanceledException` on `Task.Delay` and `yield break`.

**Test pattern:** NSubstitute mock for `IPicoApi` and `IDeviceTransport`. `PicoTc08Tests.cs` mocks `IPicoApi` in POP — translate those mock setups to NSubstitute. Verify `_pico.Received()` calls with correct channel numbers and parameters. No test file for `PicoTc08LowLevelWrapper` (it wraps native DLL).

**UAT:**
- [ ] `dotnet build Helix.Orchestration.slnx` passes
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~PicoTc08"` passes (tests PicoTc08 adapter, not the LowLevelWrapper)
- [ ] PicoTc08LowLevelWrapper does NOT extend DeviceAdapterBase
- [ ] Both PicoTc08 and PicoTc08LowLevelWrapper have `[SupportedOSPlatform("windows")]`

---

#### Task 2.3: Port IfmMasterController

**Type:** implement
**Files:**
- Write: `Helix.Orchestration.Devices/Drivers/IfmMasterController.cs`
- Write: `tests/Helix.Orchestration.Devices.Tests/Drivers/IfmMasterControllerTests.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers\IfmMasterController.cs`
- Read: `$env:USERPROFILE\source\repos\CSharpGitLab\PopNextGen\POP.Tests\DeviceTests\Drivers\IfmMasterControllerTests.cs`
- Read: `Helix.Orchestration.Devices/Interfaces/IPopIoLinkClient.cs`

**Instructions:**

**IfmMasterController specifics from RESEARCH.md:** IO-Link HTTP transport via `IPopIoLinkClient`. Constructor takes `ILogger`, `IPopIoLinkClient`, `IFile`. ~6 capabilities. has_trends YES. No `Transport.SendReceiveAsync` calls.

Note on `IFile`: This is a POP interface for file system operations. Check `IfmMasterController.cs` POP source to find all `IFile` calls. If the usage is minimal (reading `PopConfig/io_link.json`), declare `IFile` locally in `Helix.Orchestration.Devices/Interfaces/IFile.cs` with only the methods used. Add it as an additional file to write if needed.

**Driver structure — facade pattern:**

```csharp
using Helix.Orchestration.Devices.Interfaces;

namespace Helix.Orchestration.Devices.Drivers;

public sealed class IfmMasterController : DeviceAdapterBase
{
    private readonly IPopIoLinkClient _ioLink;
    private readonly IFile _file;

    public IfmMasterController(
        IPopIoLinkClient ioLink,
        IFile file,
        IDeviceTransport transport,   // stub
        ILogger<IfmMasterController> logger)
        : base(transport, logger)
    {
        _ioLink = ioLink;
        _file = file;
    }
}
```

**Port fidelity:** Read `IfmMasterController.cs` POP source. Copy every capability that `IfmMasterControllerTests.cs` exercises. Capabilities: `FindActiveSystems()`, `Open(endPoint, portNumber)`, `Close()`, plus others. Copy exact logic.

**Blocking `.Result` fix:** POP `GetTrends()` calls `_popIoLinkClient.GetAllProcessDataForDeviceAsync().Result` (blocking). In Helix `StreamTrendsAsync`, replace with `await _ioLink.GetAllProcessDataForDeviceAsync()` (proper async). This is a deliberate improvement, not an extrapolation.

**StreamTrendsAsync:** Poll every 2 seconds using `await _ioLink.GetAllProcessDataForDeviceAsync()`. Parse the returned process data into `TrendSample` objects. Use `ifmmastercontroller.{measurementType}` trend name prefix.

**io_link.json config path:** POP hardcodes `"PopConfig/io_link.json"`. In Helix, keep the same path string (injected `IFile` provides the abstraction — read the file via `_file` just as POP does). Do not change the path — copy it exactly.

**Test pattern:** NSubstitute mocks for `IPopIoLinkClient`, `IFile`, and `IDeviceTransport`. Match `IfmMasterControllerTests.cs` POP assertions.

**UAT:**
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/ --filter "FullyQualifiedName~IfmMasterController"` passes
- [ ] `IfmMasterController.cs` does NOT call `Transport.SendReceiveAsync`
- [ ] Blocking `.Result` replaced with `await` in `StreamTrendsAsync`

---

### Wave 3: DI Registration for all 8 special drivers
**Parallelizable:** No — all drivers must compile first (Waves 0–2 complete)

**Depends on:** Waves 0, 1, 2 all complete

#### Task 3.1: Register all 8 special drivers in DeviceAdapterExtensions

**Type:** implement
**Files:**
- Modify: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`
- Read: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`

**Instructions:**

Add a new private static method `AddPlan03SpecialAdapters(IServiceCollection services)` and call it from `AddMigratedDeviceAdapters()`.

Required additional usings: `using Helix.Orchestration.Devices.Interfaces;`.

**Facade drivers registration (AtlasReactionSystem, Crystalline, IfmMasterController):**

Each needs a stub `IDeviceTransport` (since no real transport is used). Register a `NullDeviceTransport` singleton (if one exists in Helix — check `Helix.Orchestration.Devices/Transports/` for a null/no-op transport). If `NullDeviceTransport` does not exist, use `Substitute.For<IDeviceTransport>()` is NOT available in production DI. Instead, create a minimal `NullDeviceTransport : IDeviceTransport` in `Helix.Orchestration.Devices/Transports/Null/NullDeviceTransport.cs` with all methods throwing `NotSupportedException("This driver does not use a transport.")`. Register it as a shared singleton for all facade drivers:

```csharp
// Check if NullDeviceTransport already exists before creating
services.AddSingleton<NullDeviceTransport>();
```

Then register facade drivers:

```csharp
// AtlasReactionSystem — File I/O facade
// Note: IDirectoryInfo, IReadCsvFile, IGetProcess require concrete implementations for production.
// Register placeholder implementations that throw NotImplementedException until real implementations are provided.
services.AddSingleton<IDirectoryInfo>(sp =>
    throw new NotImplementedException("IDirectoryInfo production implementation not registered."));
services.AddSingleton<IReadCsvFile>(sp =>
    throw new NotImplementedException("IReadCsvFile production implementation not registered."));
services.AddSingleton<IGetProcess>(sp =>
    throw new NotImplementedException("IGetProcess production implementation not registered."));
services.AddSingleton(sp => new AtlasReactionSystem(
    sp.GetRequiredService<IDirectoryInfo>(),
    sp.GetRequiredService<IReadCsvFile>(),
    sp.GetRequiredService<IGetProcess>(),
    sp.GetRequiredService<NullDeviceTransport>(),
    sp.GetRequiredService<ILogger<AtlasReactionSystem>>()));

// Crystalline — SQL facade
services.AddSingleton<ICrystallineSqlClient>(sp =>
    throw new NotImplementedException("ICrystallineSqlClient production implementation not registered."));
services.AddSingleton(sp => new Crystalline(
    sp.GetRequiredService<ICrystallineSqlClient>(),
    sp.GetRequiredService<NullDeviceTransport>(),
    sp.GetRequiredService<ILogger<Crystalline>>()));

// IfmMasterController — IO-Link HTTP facade
services.AddSingleton<IPopIoLinkClient>(sp =>
    throw new NotImplementedException("IPopIoLinkClient production implementation not registered."));
services.AddSingleton<IFile>(sp =>
    throw new NotImplementedException("IFile production implementation not registered."));
services.AddSingleton(sp => new IfmMasterController(
    sp.GetRequiredService<IPopIoLinkClient>(),
    sp.GetRequiredService<IFile>(),
    sp.GetRequiredService<NullDeviceTransport>(),
    sp.GetRequiredService<ILogger<IfmMasterController>>()));
```

**CDll driver registration:**

```csharp
// OceanViewDriver — CDll IOceanViewDriver implementation
services.AddSingleton<IOceanViewDriver, OceanViewDriver>();

// OceanViewUv and OceanViewNir — wrap IOceanViewDriver
services.AddSingleton(sp => new OceanViewUv(
    sp.GetRequiredService<IOceanViewDriver>(),
    sp.GetRequiredService<NullDeviceTransport>(),
    sp.GetRequiredService<ILogger<OceanViewUv>>()));
services.AddSingleton(sp => new OceanViewNir(
    sp.GetRequiredService<IOceanViewDriver>(),
    sp.GetRequiredService<NullDeviceTransport>(),
    sp.GetRequiredService<ILogger<OceanViewNir>>()));

// PicoTc08LowLevelWrapper — IPicoApi implementation
services.AddSingleton<IPicoApi, PicoTc08LowLevelWrapper>();

// PicoTc08 — wraps IPicoApi
services.AddSingleton(sp => new PicoTc08(
    sp.GetRequiredService<IPicoApi>(),
    sp.GetRequiredService<NullDeviceTransport>(),
    sp.GetRequiredService<ILogger<PicoTc08>>()));
```

**Capability registration in CapabilityRegistry builder:**

Register the 6 DeviceAdapterBase drivers (not OceanViewDriver or PicoTc08LowLevelWrapper — they are not adapters):

```csharp
// AtlasReactionSystem
var atlas = sp.GetRequiredService<AtlasReactionSystem>();
foreach (var cap in atlas.GetCapabilities())
{
    registry.Register($"atlasreactionsystem.{cap.Name}", new HelixAdapterCapabilityBridge(atlas));
    logger.LogInformation("Registered capability: {CapabilityName}", $"atlasreactionsystem.{cap.Name}");
}

// Crystalline
var crystalline = sp.GetRequiredService<Crystalline>();
foreach (var cap in crystalline.GetCapabilities())
{
    registry.Register($"crystalline.{cap.Name}", new HelixAdapterCapabilityBridge(crystalline));
    logger.LogInformation("Registered capability: {CapabilityName}", $"crystalline.{cap.Name}");
}

// IfmMasterController
var ifm = sp.GetRequiredService<IfmMasterController>();
foreach (var cap in ifm.GetCapabilities())
{
    registry.Register($"ifmmastercontroller.{cap.Name}", new HelixAdapterCapabilityBridge(ifm));
    logger.LogInformation("Registered capability: {CapabilityName}", $"ifmmastercontroller.{cap.Name}");
}

// OceanViewUv
var oceanViewUv = sp.GetRequiredService<OceanViewUv>();
foreach (var cap in oceanViewUv.GetCapabilities())
{
    registry.Register($"oceanviewuv.{cap.Name}", new HelixAdapterCapabilityBridge(oceanViewUv));
    logger.LogInformation("Registered capability: {CapabilityName}", $"oceanviewuv.{cap.Name}");
}

// OceanViewNir
var oceanViewNir = sp.GetRequiredService<OceanViewNir>();
foreach (var cap in oceanViewNir.GetCapabilities())
{
    registry.Register($"oceanviewnir.{cap.Name}", new HelixAdapterCapabilityBridge(oceanViewNir));
    logger.LogInformation("Registered capability: {CapabilityName}", $"oceanviewnir.{cap.Name}");
}

// PicoTc08
var pico = sp.GetRequiredService<PicoTc08>();
foreach (var cap in pico.GetCapabilities())
{
    registry.Register($"picotc08.{cap.Name}", new HelixAdapterCapabilityBridge(pico));
    logger.LogInformation("Registered capability: {CapabilityName}", $"picotc08.{cap.Name}");
}
```

**UAT:**
- [ ] `dotnet build Helix.Orchestration.slnx` passes
- [ ] `DeviceAdapterExtensions.cs` contains `AddPlan03SpecialAdapters` method call
- [ ] Capability prefixes `atlasreactionsystem.`, `crystalline.`, `ifmmastercontroller.`, `oceanviewuv.`, `oceanviewnir.`, `picotc08.` all appear in CapabilityRegistry builder
- [ ] `OceanViewDriver` and `PicoTc08LowLevelWrapper` are NOT in the CapabilityRegistry builder (they are not adapters)

---

## Overall UAT

- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/` passes (all Plan 03 tests pass, zero failures)
- [ ] `dotnet build Helix.Orchestration.slnx` passes
- [ ] All 7 interface files exist in `Helix.Orchestration.Devices/Interfaces/`
- [ ] 6 DeviceAdapterBase driver files exist in `Helix.Orchestration.Devices/Drivers/` (AtlasReactionSystem, Crystalline, IfmMasterController, OceanViewUv, OceanViewNir, PicoTc08)
- [ ] 2 non-adapter files exist (OceanViewDriver, PicoTc08LowLevelWrapper) — NOT DeviceAdapterBase subclasses
- [ ] 6 test files exist in `tests/Helix.Orchestration.Devices.Tests/Drivers/`
- [ ] None of the facade drivers call `Transport.SendReceiveAsync`
- [ ] No capabilities extrapolated beyond POP source + tests

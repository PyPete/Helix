# Phase 1: Technical Debt Resolution - Pattern Map

**Mapped:** 2026-05-26
**Files analyzed:** 8 deliverables (A1–A8)
**Analogs found:** 8 / 8

---

## File Classification

| Target File (to create or modify) | Role | Data Flow | Closest Analog | Match Quality |
|-----------------------------------|------|-----------|----------------|---------------|
| `Helix.Orchestration.Recorder/Worker.cs` | hosted-service (delete/replace) | event-driven | `Helix.Orchestration.Recorder/Services/MqttRecorderService.cs` | exact |
| `Helix.Orchestration.Recorder/Program.cs` | config/host | request-response | existing `Helix.Orchestration.Recorder/Program.cs` (already correct) | exact |
| `Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs` | interface | file-I/O | existing (already canonical — keep this one) | exact |
| `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` | DI-extension | CRUD | `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs` | role-match |
| `Helix.Orchestration.Agent/Program.cs` | config/host | request-response | existing file (targeted edit) | exact |
| `Helix.Orchestration.Agent/Transport/OpcTransport.cs` | transport | request-response | `Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs` | exact |
| `Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj` | config | — | existing file (targeted edit) | exact |
| `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs` | utility | — | `Activator.CreateInstance<T>()` (framework API) | exact |
| `Helix.Orchestration.SiteServer/Program.cs` | config/host | CRUD | `Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs` | role-match |
| `Helix.Orchestration.slnx` | config | — | existing file (targeted edit) | exact |

---

## Pattern Assignments

### A1 — Merge Recorder: replace `Helix.Orchestration.Recorder/Worker.cs`

**Situation:** The solution's Recorder project contains only a placeholder `Worker.cs` that loops with `Task.Delay(30s)`. The full MQTT + writer pipeline is already implemented in the same project tree as separate services.

**Target file to change:**
- `Helix.Orchestration.Recorder/Worker.cs` — DELETE this file entirely; it is replaced by the two hosted services below which are already present.

**Already correct (no changes needed):**
- `Helix.Orchestration.Recorder/Program.cs` — already registers `MqttRecorderService` and `RecorderWriterService`; does NOT register the stub `Worker`. The stub `Worker.cs` is the sole problem.

**Stub pattern to DELETE** (`Helix.Orchestration.Recorder/Worker.cs`, lines 7–44):
```csharp
// This is the placeholder — DELETE this class
public class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Recorder service is running at {Time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // ← 30s delay loop, not MQTT
        }
    }
}
```

**Real implementation already present:**
- `Helix.Orchestration.Recorder/Services/MqttRecorderService.cs` — `BackgroundService` that connects to MQTT, handles reconnect, enqueues `RecordedEvent` into `Channel<RecordedEvent>` (lines 17–86)
- `Helix.Orchestration.Recorder/Services/RecorderWriterService.cs` — `BackgroundService` that drains channel, batches by `(experimentId, trendId)`, flushes to `IObjectStorageSink` as NDJSON (lines 17–136)
- `Helix.Orchestration.Recorder/Models/RecordedEvent.cs` — record type with `TryParseFromMqttMessage` (lines 7–54)
- `Helix.Orchestration.Recorder/Configuration/RecorderOptions.cs` — `RecorderOptions` class (lines 1–21)

**DI registration pattern** (`Helix.Orchestration.Recorder/Program.cs`, lines 37–38):
```csharp
builder.Services.AddHostedService<MqttRecorderService>();
builder.Services.AddHostedService<RecorderWriterService>();
// Worker is NOT registered — confirm no AddHostedService<Worker>() exists after the delete
```

**Gotcha:** `Worker.cs` must only be deleted; it is not referenced from `Program.cs`. Verify with grep after deletion that no stray reference remains.

---

### A2 — Unify `IObjectStorageSink`: keep Infrastructure version, remove root-level duplicate

**Canonical interface (KEEP):**
`Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs` (lines 6–22)

```csharp
namespace Helix.Orchestration.Infrastructure.Storage;

public interface IObjectStorageSink
{
    Task UploadObjectAsync(string bucketName, string objectName, Stream stream, CancellationToken cancellationToken = default);
    Task DownloadObjectAsync(string bucketName, string objectName, Stream outputStream, CancellationToken cancellationToken = default);
    Task<bool> ObjectExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    Task DeleteObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    // WriteAsync is already present as the key convenience method:
    Task WriteAsync(string key, Stream stream, string contentType, CancellationToken cancellationToken = default);
}
```

**Canonical implementation (KEEP):**
`Helix.Orchestration.Infrastructure/Storage/MinioObjectStorageSink.cs`
- Uses `AWSSDK.S3` (`AmazonS3Client`, `AmazonS3Config`) — already in `Helix.Orchestration.Infrastructure.csproj`
- `WriteAsync` implementation (lines 101–118): uses `_options.DefaultBucket ?? "helix-recordings"`, `PutObjectRequest` with `ContentType`
- `MinioOptions` class: `Helix.Orchestration.Infrastructure/Storage/MinioOptions.cs`

**What the Recorder already does correctly** (`Helix.Orchestration.Recorder/Program.cs`, lines 31–35):
```csharp
builder.Services.AddSingleton<IObjectStorageSink>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
    return new MinioObjectStorageSink(opts);
});
```

**Root-level duplicate to investigate and remove:**
The CONCERNS.md notes a diverged root-level `Helix.Orchestration.Infrastructure/` outside the solution directory. Since the solution's Recorder already imports `Helix.Orchestration.Infrastructure.Storage` (confirmed: `Helix.Orchestration.Recorder.csproj` has `ProjectReference` to Infrastructure), the root-level duplicate is unreferenced and should simply be deleted.

**Gotcha:** The CONCERNS.md says the root-level version used the `Minio` NuGet SDK (not AWSSDK.S3) with a different `MinioOptions` shape. Do NOT pull that client into the solution — the solution uses AWSSDK.S3 exclusively.

---

### A3 — Populate `CapabilityRegistry` in `DeviceAdapterExtensions`

**Target file:**
`Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`

**Current empty registry** (lines 17–21):
```csharp
services.AddSingleton<CapabilityRegistry>(sp =>
{
    var registry = new CapabilityRegistry();
    // NOTHING IS REGISTERED — fix goes here
    return registry;
});
```

**`CapabilityRegistry.Register` signature** (`Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs`, lines 22–26):
```csharp
public void Register(string capabilityName, ICapabilityInvoker invoker)
{
    if (!_invokers.TryAdd(capabilityName, invoker))
        throw new ArgumentException($"Capability '{capabilityName}' is already registered.", nameof(capabilityName));
}
```

**Pattern to follow — `DeviceCapabilityAdapter<TDevice>` as the `ICapabilityInvoker`:**
`Helix.Orchestration.Agent/Adapters/Devices/DeviceCapabilityAdapter.cs` (lines 6–9):
```csharp
public sealed class DeviceCapabilityAdapter<TDevice>(string capabilityName, TDevice device)
    : ICapabilityInvoker
    where TDevice : class, IDevice
```

**For Phase 1 (placeholder registration), the pattern is:**
```csharp
services.AddSingleton<CapabilityRegistry>(sp =>
{
    var registry = new CapabilityRegistry();
    var logger = sp.GetRequiredService<ILogger<CapabilityRegistry>>();
    logger.LogInformation("CapabilityRegistry initialized — no adapters registered (Phase 1 placeholder).");
    // Phase 2 will register: registry.Register("SetTemperature", new DeviceCapabilityAdapter<IReactionPlatform>("SetTemperature", device));
    return registry;
});
```

**Logging pattern to confirm startup** — use `ILogger<CapabilityRegistry>` injected via `sp.GetRequiredService<ILogger<CapabilityRegistry>>()` inside the factory lambda.

**Gotcha — DI lifetime mismatch (CONCERNS.md, line 58):** Transports are currently registered as `Transient` but the `CapabilityRegistry` is `Singleton`. When a device adapter holds a transport reference, it becomes effectively singleton. For Phase 1 (placeholder only), this does not matter. Phase 2 will need to change transport registrations to `Singleton`.

**Gotcha — namespace:** `ICapabilityInvoker` and `CapabilityInvocationResult` from `Helix.Orchestration.Core.Capabilities` are what `CapabilityRegistry` uses. `Helix.Orchestration.Devices.Abstractions` defines its own `CapabilityInvocationResult` with `Successful(...)`. The Agent's `DeviceCapabilityAdapter.cs` already uses `Core.Capabilities.CapabilityInvocationResult.Succeeded(...)` (line 35) — follow this, not the Devices version.

---

### A4 — Replace `InMemoryFunctionStore` with Cleipnir PostgreSQL store

**Target file:**
`Helix.Orchestration.Agent/Program.cs` (lines 56–58)

**Current pattern to REPLACE:**
```csharp
// FROM (lines 56–58):
builder.Services.AddSingleton(new InMemoryFunctionStore());
builder.Services.AddSingleton<FunctionsRegistry>(sp =>
    new FunctionsRegistry(sp.GetRequiredService<InMemoryFunctionStore>()));
```

**Replacement pattern:**
```csharp
// TO:
var connectionString = builder.Configuration.GetConnectionString("HelixOrchestration")
    ?? throw new InvalidOperationException("Connection string 'HelixOrchestration' is required for Cleipnir PostgreSQL store.");

var postgresFunctionStore = new PostgreSqlFunctionStore(connectionString);
await postgresFunctionStore.Initialize();

builder.Services.AddSingleton<IFunctionStore>(postgresFunctionStore);
builder.Services.AddSingleton<FunctionsRegistry>(sp =>
    new FunctionsRegistry(sp.GetRequiredService<IFunctionStore>()));
```

**NuGet package to add to `Helix.Orchestration.Agent.csproj`:**
```xml
<PackageReference Include="Cleipnir.ResilientFunctions.PostgreSQL" Version="4.*" />
```

**Connection string source:** `appsettings.json` key `ConnectionStrings:HelixOrchestration` — same key used by `InfrastructureServiceExtensions.AddHelixInfrastructure()` (line 11 of `InfrastructureServiceExtensions.cs`). The `docker-compose.yml` PostgreSQL service (lines 29–31) provides: host=localhost, db=helix_orchestration, user=helix, password=helix_dev_password.

**Connection string to add to `Helix.Orchestration.Agent/appsettings.json`:**
```json
"ConnectionStrings": {
  "HelixOrchestration": "Host=localhost;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password"
}
```

**Import to add:**
```csharp
using Cleipnir.ResilientFunctions.Storage;         // IFunctionStore
using Cleipnir.ResilientFunctions.PostgreSQL;       // PostgreSqlFunctionStore
```

**Remove import:**
```csharp
// REMOVE: using Cleipnir.ResilientFunctions.Storage; (only if InMemoryFunctionStore was the only usage)
// Keep FunctionsRegistry import from Cleipnir.ResilientFunctions
```

**Gotcha:** `PostgreSqlFunctionStore.Initialize()` is async and must be awaited before the host starts. Call it before `builder.Build()`. The `Program.cs` top-level statements support `await` directly.

**Gotcha:** `PostgreSqlFunctionStore` is in the `Cleipnir.ResilientFunctions.PostgreSQL` package (separate from the base `Cleipnir.ResilientFunctions`). The NuGet search confirms the package ID is `Cleipnir.ResilientFunctions.PostgreSQL` at version 4.2.5.

---

### A5 — Remove `Opc.UaFx.Advanced`; migrate `OpcTransport.cs` to use `OpcDeviceTransport`/`OpcFoundationSession`

**Files involved:**
- **DELETE (logic):** `Helix.Orchestration.Agent/Transport/OpcTransport.cs` — uses `Opc.UaFx.Client.OpcClient` (line 2)
- **REMOVE (reference):** `Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj` line 17: `<PackageReference Include="Opc.UaFx.Advanced" Version="2.9.1" />`
- **ADOPT:** `Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs` — `IDeviceTransport` implementation using OPC Foundation
- **ADOPT:** `Helix.Orchestration.Devices/Transports/Opc/OpcFoundationSession.cs` — `IOpcSession` implementation using OPC Foundation

**Current `OpcTransport` interface** (`Helix.Orchestration.Devices/Transport/IOpcTransport.cs`):

Check this file for the interface shape:

```csharp
// IOpcTransport.cs is at: Helix.Orchestration.Devices/Transport/IOpcTransport.cs
// The DeviceAdapterExtensions registers:
services.AddTransient<IOpcTransport, OpcTransport>(); // line 14 of DeviceAdapterExtensions.cs
```

**Target registration change in `DeviceAdapterExtensions.cs` (line 14):**
```csharp
// FROM:
services.AddTransient<IOpcTransport, OpcTransport>(); // OpcTransport uses Opc.UaFx

// TO (two options — choose based on what IOpcTransport requires):
// Option A: Delete the IOpcTransport registration entirely if OpcDeviceTransport supersedes it
// Option B: Create a thin adapter that wraps OpcDeviceTransport behind IOpcTransport
```

**The new OPC UA transport to use directly** (`Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs`):
- Constructor: `OpcDeviceTransport(OpcTransportOptions options, ILogger<OpcDeviceTransport> logger)` (line 33)
- Implements `IDeviceTransport` (not `IOpcTransport`) — this is the DI mismatch to resolve
- Operations: `ConnectAsync`, `DisconnectAsync`, `SendReceiveAsync` with JSON `OpcCommand` envelope (operation: read/write/call/subscribe)
- Session created via `OpcFoundationSessionFactory` → `OpcFoundationSession`

**`OpcTransportOptions` shape** — located at `Helix.Orchestration.Devices/Transports/Opc/OpcTransportOptions.cs` (read if needed for full field list; used in `OpcDeviceTransport` constructor line 43).

**Import to add to Agent.csproj** (already present — Agent already has `ProjectReference` to Devices):
```xml
<!-- Already present in Helix.Orchestration.Agent.csproj line 28 -->
<ProjectReference Include="..\Helix.Orchestration.Devices\Helix.Orchestration.Devices.csproj" />
```

**Import to REMOVE from Agent.csproj:**
```xml
<PackageReference Include="Opc.UaFx.Advanced" Version="2.9.1" />
```

**Gotcha — `AutoAcceptUntrustedCertificates = true`:** `OpcDeviceTransport.cs` line 80 sets this in the `ApplicationConfiguration`. This is a known security concern (CONCERNS.md line 65). For Phase 1, the task is just the library swap; the security flag is explicitly noted as a Phase 3 follow-up.

**Gotcha — interface mismatch:** `IOpcTransport` (in `Helix.Orchestration.Devices/Transport/`) may have different method signatures than `IDeviceTransport`. The Agent's `DeviceAdapterExtensions` currently registers `IOpcTransport`; if no code in the Agent actually calls `IOpcTransport` methods directly (only the registry holds it), you may be able to simply remove the `IOpcTransport` registration and register `OpcDeviceTransport` as `IDeviceTransport` alongside the others.

---

### A6 — Replace `FormatterServices.GetUninitializedObject`

**Target file:**
`Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs`

**Exact location:** line 112

**Current deprecated code** (lines 109–115):
```csharp
private static TOptions BuildOptions<TOptions>(Action<TOptions> configureOptions)
    where TOptions : class
{
    var options = (TOptions)FormatterServices.GetUninitializedObject(typeof(TOptions));
    configureOptions(options);
    return options;
}
```

**Replacement pattern:**
```csharp
private static TOptions BuildOptions<TOptions>(Action<TOptions> configureOptions)
    where TOptions : class, new()  // ADD: new() constraint
{
    var options = Activator.CreateInstance<TOptions>();  // REPLACE the FormatterServices call
    configureOptions(options);
    return options;
}
```

**Import to REMOVE** (line 1 of `DevicesServiceCollectionExtensions.cs`):
```csharp
using System.Runtime.Serialization; // remove — only used for FormatterServices
```

**Gotcha — `new()` constraint:** Adding `new()` to the generic constraint requires that all `TOptions` types passed to `BuildOptions` have a public parameterless constructor. Check all call sites in the same file:
- `AddHelixSerialAdapter<TAdapter>` calls `BuildOptions(configureOptions)` with `SerialTransportOptions`
- `AddHelixModbusAdapter<TAdapter>` calls `BuildOptions(configureOptions)` with `ModbusTransportOptions`  
- `AddHelixTcpAdapter<TAdapter>` calls `BuildOptions(configureOptions)` with `TcpTransportOptions`
- `AddHelixOpcAdapter<TAdapter>` calls `BuildOptions(configureOptions)` with `OpcTransportOptions`
- `AddHelixCDllAdapter<TAdapter>` calls `BuildOptions(configureOptions)` with `CDllTransportOptions`

Verify each options class has a public parameterless constructor before adding `new()`. If any options type uses a primary constructor with required parameters, use `(TOptions)Activator.CreateInstance(typeof(TOptions))` without the `new()` constraint as a fallback.

---

### A7 — Wire `IExperimentRepository` + `ICampaignRepository` in SiteServer

**Target file:**
`Helix.Orchestration.SiteServer/Program.cs`

**The registration method to call:**
`Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs` (lines 9–19):
```csharp
public static IServiceCollection AddHelixInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("HelixOrchestration")
        ?? throw new InvalidOperationException("Connection string 'HelixOrchestration' is not configured.");

    services.AddSingleton(new DbConnectionFactory(connectionString));
    services.AddScoped<IExperimentRepository, PostgresExperimentRepository>();
    services.AddScoped<ICampaignRepository, PostgresCampaignRepository>();

    return services;
}
```

**Line to add to `SiteServer/Program.cs`** (after the existing service registrations, before `var app = builder.Build()`):
```csharp
// Register PostgreSQL repositories for campaigns and experiments
builder.Services.AddHelixInfrastructure(builder.Configuration);
```

**Using directive to add:**
```csharp
using Helix.Orchestration.Infrastructure.Persistence;
```

**Connection string to add to `Helix.Orchestration.SiteServer/appsettings.json`:**
```json
"ConnectionStrings": {
  "HelixOrchestration": "Host=localhost;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password"
}
```

**SiteServer.csproj already has the project reference** (line 21):
```xml
<ProjectReference Include="..\Helix.Orchestration.Infrastructure\Helix.Orchestration.Infrastructure.csproj" />
```

**Gotcha — `CampaignManager` uses in-memory `ConcurrentDictionary` internally.** `ICampaignRepository` and `IExperimentRepository` are injected into endpoints, not into `CampaignManager` itself. Check endpoint implementations before assuming they use the repository. The CONCERNS.md notes "CampaignManager in SiteServer uses only in-memory ConcurrentDictionary; IExperimentRepository/ICampaignRepository from Infrastructure are never injected" — this implies the wiring is needed both in DI registration AND in the endpoint handler classes that currently bypass the repository.

**Gotcha — `AddHelixInfrastructure` throws** if `ConnectionStrings:HelixOrchestration` is missing from config. The existing `appsettings.json` for SiteServer has no `ConnectionStrings` section. Add it to `appsettings.Development.json` (for dev override) so production requires explicit configuration.

---

### A8 — Fix `.slnx` build configuration

**Target file:**
`Helix.Orchestration.slnx` (repo root, lines 1–39)

**Current broken entries** — these have `<Build Solution="Debug|*" Project="false" />` which excludes them from Debug builds:

```xml
<!-- CURRENTLY EXCLUDED from Debug builds — lines 4, 7, 10, 15, 18, 23, 26, 29, 33 -->
<Project Path="tests/Helix.Orchestration.Devices.Tests/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
<Project Path="tests/Helix.Orchestration.Infrastructure.Tests/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
<Project Path="tests/Helix.Orchestration.Recorder.Tests/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
<Project Path="Helix.Orchestration.Agent/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
<Project Path="Helix.Orchestration.Contracts/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
<Project Path="Helix.Orchestration.Infrastructure/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
<Project Path="Helix.Orchestration.Recorder/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
<Project Path="Helix.Orchestration.Simulation/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
<Project Path="Helix.Orchestration.SiteServer/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
<Project Path="tests/Helix.Orchestration.Agent.Tests/...">
  <Build Solution="Debug|*" Project="false" />  <!-- REMOVE this element -->
</Project>
```

**Currently included without restrictions (KEEP AS IS):**
```xml
<Project Path="tests/Helix.Orchestration.SiteServer.Tests/..." />   <!-- no Build exclusion — correct -->
<Project Path="Helix.Orchestration.Core/..." />                      <!-- no Build exclusion — correct -->
<Project Path="Helix.Orchestration.Devices/..." />                   <!-- no Build exclusion — correct -->
<Project Path="tests/Helix.Orchestration.Core.Tests/..." />          <!-- no Build exclusion — correct -->
```

**Target state after fix** — every `<Project>` element should have no `<Build>` child element (or be left as a self-closing tag):
```xml
<Solution>
  <Folder Name="/tests/">
    <Project Path="tests/Helix.Orchestration.Devices.Tests/Helix.Orchestration.Devices.Tests.csproj" />
    <Project Path="tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj" />
    <Project Path="tests/Helix.Orchestration.Recorder.Tests/Helix.Orchestration.Recorder.Tests.csproj" />
    <Project Path="tests/Helix.Orchestration.SiteServer.Tests/Helix.Orchestration.SiteServer.Tests.csproj" />
  </Folder>
  <Project Path="Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj" />
  <Project Path="Helix.Orchestration.Contracts/Helix.Orchestration.Contracts.csproj" />
  <Project Path="Helix.Orchestration.Core/Helix.Orchestration.Core.csproj" />
  <Project Path="Helix.Orchestration.Devices/Helix.Orchestration.Devices.csproj" />
  <Project Path="Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj" />
  <Project Path="Helix.Orchestration.Recorder/Helix.Orchestration.Recorder.csproj" />
  <Project Path="Helix.Orchestration.Simulation/Helix.Orchestration.Simulation.csproj" />
  <Project Path="Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj" />
  <Project Path="tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj" />
  <Project Path="tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj" />
</Solution>
```

**Gotcha:** After enabling all projects in Debug, `dotnet build` will expose any compile errors that were previously hidden. Expect errors from A5 (if `Opc.UaFx.Advanced` removal is done before this) and potentially from A6 (`FormatterServices`). Complete A5 and A6 before validating A8's build success.

---

## Shared Patterns

### Hosted Service Pattern
**Source:** `Helix.Orchestration.Recorder/Services/MqttRecorderService.cs`
**Apply to:** A1 (understanding what the real recorder services do vs the stub)

```csharp
// BackgroundService base class with reconnect loop:
public sealed class MqttRecorderService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // connect + await disconnect
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection lost — reconnecting in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
```

### DI Extension Method Pattern
**Source:** `Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs`
**Apply to:** A7 (calling `AddHelixInfrastructure`)

```csharp
// Extension method on IServiceCollection, takes IConfiguration:
public static IServiceCollection AddHelixInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
```

### Serilog Bootstrap Logger Pattern
**Source:** `Helix.Orchestration.Agent/Program.cs` (lines 21–25) and `Helix.Orchestration.Recorder/Program.cs` (lines 9–11)
**Apply to:** any future service host

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try { await host.RunAsync(); return 0; }
catch (Exception ex) { Log.Fatal(ex, "..."); return 1; }
finally { await Log.CloseAndFlushAsync(); }
```

### IOptions<T> Configuration Pattern
**Source:** `Helix.Orchestration.Recorder/Program.cs` (lines 17–21)
**Apply to:** A4 (connection string in Agent `appsettings.json`)

```csharp
builder.Services.Configure<RecorderOptions>(
    builder.Configuration.GetSection(RecorderOptions.SectionName));
```

---

## No Analog Found

| Deliverable | Gap | Reason |
|-------------|-----|--------|
| A4 — Cleipnir PostgreSQL store API | `PostgreSqlFunctionStore` exact initialization API | Package not yet in solution; must check Cleipnir 4.x API at `Cleipnir.ResilientFunctions.PostgreSQL` package before coding. Known class name from NuGet search. |
| A5 — `IOpcTransport` vs `IDeviceTransport` bridge | How to satisfy `IOpcTransport` without `Opc.UaFx` | `IOpcTransport` was authored for the old Agent transport layer; `OpcDeviceTransport` implements `IDeviceTransport` instead. May require reading `Helix.Orchestration.Devices/Transport/IOpcTransport.cs` to determine if interface can be removed or if an adapter wrapper is needed. |

---

## Metadata

**Analog search scope:** All `.cs` and `.csproj` files under `Helix.Orchestration.*` and `tests/`
**Files scanned:** ~50 files read directly; grep across full tree for key type names
**Pattern extraction date:** 2026-05-26

**Recommended execution order for Phase 1:**
1. A8 (slnx) — enable builds first to catch errors early (but expect A5/A6 failures until those are fixed)
2. A6 (FormatterServices) — one-line fix, unblocks A8 build
3. A5 (OPC UA swap) — removes commercial package, unblocks A8 build
4. A1 (Recorder stub delete) — delete one file, confirm Program.cs already correct
5. A2 (IObjectStorageSink) — verify no divergent copy is referenced, delete root-level duplicate
6. A3 (CapabilityRegistry placeholder) — add logging line to empty factory lambda
7. A4 (Cleipnir PostgreSQL) — add NuGet package + connection string + store initialization
8. A7 (SiteServer DI) — add one `AddHelixInfrastructure` call + connection string

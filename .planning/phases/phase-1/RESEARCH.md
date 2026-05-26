# Phase 1: Technical Debt Resolution — Research

**Researched:** 2026-05-26
**Domain:** C# / .NET 10, Cleipnir ResilientFunctions, MinIO/S3 storage, OPC UA, .slnx solution format
**Confidence:** HIGH

---

## Executive Summary

All eight deliverables (A1–A8) map directly to existing code in the repository; none require new
architectural design. The primary research unknowns were the Cleipnir PostgreSQL store API, the
correct Minio SDK approach, and the `.slnx` build-exclusion syntax. All three are now fully
resolved with authoritative sources. One material discovery: adding
`Cleipnir.ResilientFunctions.PostgreSQL` at 4.2.5 mandates bumping the base
`Cleipnir.ResilientFunctions` package from 4.0.0 to 4.2.5 in both `Core.csproj` and
`Agent.csproj` — this is a firm dependency constraint, not an optional upgrade.

**Primary recommendation:** Execute in order A8 → A6 → A5 → A1 → A2 → A3 → A4 → A7 to surface
compile errors incrementally. Each change is a targeted file edit or package swap; no new classes
or architectural patterns need to be invented.

---

## Codebase State Summary (Verified by Direct Read)

| Item | Actual State | Matches Brief? |
|------|-------------|----------------|
| `Recorder/Worker.cs` | Exists as dead code (not registered in Program.cs); Program.cs already correct | Yes |
| `Recorder/Program.cs` | Registers `MqttRecorderService` + `RecorderWriterService`; Worker NOT registered | Yes |
| `IObjectStorageSink` | ONE canonical version in `Infrastructure/Storage/`; already has `WriteAsync(key, stream, contentType, ct)` | Yes |
| `MinioObjectStorageSink` | Uses `AWSSDK.S3` (not Minio SDK); A2 task = verify no second diverged copy remains | Clarified below |
| `DeviceAdapterExtensions.AddMigratedDeviceAdapters()` | CapabilityRegistry created but empty (no `Register()` calls) | Yes |
| Agent `Program.cs` | Uses `InMemoryFunctionStore` + `FunctionsRegistry(InMemoryFunctionStore)` | Yes |
| Agent `OpcTransport.cs` | Uses `Opc.UaFx.Client.OpcClient` (one `using` + entire implementation) | Yes |
| `FormatterServices.GetUninitializedObject` | Present at line 112 of `DevicesServiceCollectionExtensions.cs` | Yes |
| SiteServer `Program.cs` | No `AddHelixInfrastructure` call; Infrastructure project IS referenced | Yes |
| `.slnx` file | 10 of 14 projects have `<Build Solution="Debug|*" Project="false" />` child elements | Yes |

---

## A1 — Merge Recorder (Delete Worker.cs)

### Finding

`Worker.cs` is a dead file. It is defined in the namespace but never registered with
`AddHostedService<Worker>()` in `Program.cs`. The real pipeline is already wired:

```csharp
// Program.cs lines 37-38 — already correct, no changes needed
builder.Services.AddHostedService<MqttRecorderService>();
builder.Services.AddHostedService<RecorderWriterService>();
```

The full service classes — `MqttRecorderService`, `RecorderWriterService`, `RecordedEvent`,
`RecorderOptions` — all exist and compile correctly. [VERIFIED: direct codebase read]

### Action

Delete `Helix.Orchestration.Recorder/Worker.cs` only. No other file changes for A1.

### Gotcha

After deletion, run `grep -r "Worker" Helix.Orchestration.Recorder/ --include="*.cs"` to confirm
no stray references remain.

---

## A2 — IObjectStorageSink Unification

### Finding: Current State (Different from Phase Brief)

The phase brief describes two diverged `IObjectStorageSink` interfaces (one with Minio SDK, one
with AWSSDK.S3). That divergence existed in the pre-flatten state (confirmed via `git show
53a0eeb`). After the flatten commit, there is **one** canonical interface in
`Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs`. [VERIFIED: direct codebase read]

The canonical interface already has all five methods including `WriteAsync(key, stream, contentType, ct)`:

```csharp
namespace Helix.Orchestration.Infrastructure.Storage;

public interface IObjectStorageSink
{
    Task UploadObjectAsync(string bucketName, string objectName, Stream stream, CancellationToken ct = default);
    Task DownloadObjectAsync(string bucketName, string objectName, Stream outputStream, CancellationToken ct = default);
    Task<bool> ObjectExistsAsync(string bucketName, string objectName, CancellationToken ct = default);
    Task DeleteObjectAsync(string bucketName, string objectName, CancellationToken ct = default);
    Task WriteAsync(string key, Stream stream, string contentType, CancellationToken ct = default);
}
```

`MinioObjectStorageSink` implements this interface using `AWSSDK.S3` (`AmazonS3Client`). This is
the canonical implementation. [VERIFIED: direct codebase read of MinioObjectStorageSink.cs]

### Decision: AWSSDK.S3 vs Minio SDK

The "switch to Minio SDK" described in the brief refers to the deleted root-level implementation.
The PATTERNS.md analysis (already in `.planning/phases/phase-1/PATTERNS.md`) confirms: **keep
AWSSDK.S3**. The AWSSDK.S3 S3-compatible API works correctly with MinIO using `ForcePathStyle =
true`. Switching to the Minio SDK would add churn with no functional benefit for Phase 1.

Minio 7.0.0 (released November 2025) has a completely new API that breaks the 6.x builder
pattern (`PutObjectArgs.WithStreamData(...)`). Minio 6.x uses:

```csharp
// Minio 6.x PutObjectArgs pattern (NOT used — kept for reference only)
var putArgs = new PutObjectArgs()
    .WithBucket(bucket)
    .WithObject(key)
    .WithStreamData(stream)
    .WithObjectSize(stream.Length)
    .WithContentType(contentType);
await minioClient.PutObjectAsync(putArgs, ct);
```

Minio 7.0.0 switched to a different method:
```csharp
// Minio 7.0.0 pattern (NOT used — kept for reference only)
await minioClient.PutObjectAsync(bucket, key, stream, options: new PutObjectOptions { ContentType = contentType }, ct);
```

[CITED: github.com/minio/minio-dotnet — v7 README and IMinioClient interface]

### Action for A2

A2 is effectively a verification step only. Confirm no second `IObjectStorageSink` file exists
outside `Helix.Orchestration.Infrastructure/`, then close the deliverable. No code changes to
the interface or implementation are needed. [VERIFIED: `find` across repo confirms one file only]

---

## A3 — CapabilityRegistry Population

### Finding

`DeviceAdapterExtensions.AddMigratedDeviceAdapters()` creates a `CapabilityRegistry` but calls
no `Register()` methods. This makes every capability invocation return a "not registered" failure.
[VERIFIED: direct read of `DeviceAdapterExtensions.cs` lines 17–21]

`CapabilityRegistry.Register(string capabilityName, ICapabilityInvoker invoker)` is the
registration point. Phase 1 requires at least a placeholder registration to prove the wiring
compiles and runs. [VERIFIED: direct read of `CapabilityRegistry.cs`]

### Pattern

```csharp
services.AddSingleton<CapabilityRegistry>(sp =>
{
    var registry = new CapabilityRegistry();
    var logger = sp.GetRequiredService<ILogger<CapabilityRegistry>>();
    // Phase 1 placeholder: demonstrates wiring is functional
    logger.LogInformation("CapabilityRegistry initialized — 0 adapters registered (Phase 1 placeholder).");
    // Phase 2 will register real adapters, e.g.:
    // registry.Register("SetTemperature", new DeviceCapabilityAdapter<IReactionPlatform>(...));
    return registry;
});
```

### Gotcha — DI Lifetime Mismatch

Transports are registered as `Transient`. If a future `DeviceCapabilityAdapter` captures a
transport at registration time (in the singleton factory lambda), the transport becomes effectively
singleton. Phase 1 placeholder avoids this because no adapters are registered. Phase 2 will need
to register transports as `Singleton` before wiring them into adapters.

---

## A4 — Cleipnir PostgreSQL Store

### Packages (VERIFIED: NuGet registry + direct nuspec)

| Package | Required Version | NuGet Latest | Downloads |
|---------|-----------------|--------------|-----------|
| `Cleipnir.ResilientFunctions` | 4.2.5 (upgrade from 4.0.0) | 4.2.5 | 35,697 |
| `Cleipnir.ResilientFunctions.PostgreSQL` | 4.2.5 | 4.2.5 | 17,123 |

**CRITICAL — Version Bump Required:** `Cleipnir.ResilientFunctions.PostgreSQL` 4.2.5 has a hard
NuGet dependency on `Cleipnir.ResilientFunctions >= 4.2.5`. The solution currently pins the base
package at 4.0.0 in both `Helix.Orchestration.Core.csproj` and
`Helix.Orchestration.Agent.csproj`. Both files must be updated to `Version="4.*"` (or `"4.2.5"`)
when adding the PostgreSQL package. [VERIFIED: nuspec from api.nuget.org confirms dependency]

The `FunctionsRegistry` constructor signature is identical between 4.0.0 and 4.2.5 — no
call-site changes beyond the store type swap. [VERIFIED: README.md from both cached package
versions in NuGet global-packages]

### Class and API

```csharp
// Namespace: Cleipnir.ResilientFunctions.PostgreSQL
// Constructor:
public PostgreSqlFunctionStore(string connectionString, string tablePrefix = "")

// Initialize (creates tables + indexes if not present — idempotent):
public async Task Initialize()

// Implements: IFunctionStore
```

[VERIFIED: github.com/stidsborg/Cleipnir.ResilientFunctions — PostgreSqlFunctionStore.cs source]

### Migration Pattern

Replace these three lines in `Agent/Program.cs` (current lines 56–58):

```csharp
// REMOVE:
builder.Services.AddSingleton(new InMemoryFunctionStore());
builder.Services.AddSingleton<FunctionsRegistry>(sp =>
    new FunctionsRegistry(sp.GetRequiredService<InMemoryFunctionStore>()));
```

Replace with:

```csharp
// ADD (before builder.Build()):
var connectionString = builder.Configuration.GetConnectionString("HelixOrchestration")
    ?? throw new InvalidOperationException("Connection string 'HelixOrchestration' is required.");

var postgresFunctionStore = new PostgreSqlFunctionStore(connectionString);
await postgresFunctionStore.Initialize();   // Must be awaited — creates tables on first run

builder.Services.AddSingleton<IFunctionStore>(postgresFunctionStore);
builder.Services.AddSingleton<FunctionsRegistry>(sp =>
    new FunctionsRegistry(sp.GetRequiredService<IFunctionStore>()));
```

Add to `Agent/appsettings.json`:

```json
"ConnectionStrings": {
  "HelixOrchestration": "Host=localhost;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password"
}
```

Add to `Agent.csproj`:

```xml
<PackageReference Include="Cleipnir.ResilientFunctions.PostgreSQL" Version="4.*" />
```

Bump existing base package in `Agent.csproj` and `Core.csproj`:

```xml
<!-- Was: Version="4.0.0" — Change to: -->
<PackageReference Include="Cleipnir.ResilientFunctions" Version="4.*" />
```

Add usings to `Agent/Program.cs`:

```csharp
using Cleipnir.ResilientFunctions.Storage;       // IFunctionStore
using Cleipnir.ResilientFunctions.PostgreSQL;     // PostgreSqlFunctionStore
```

Remove using (if only used by InMemoryFunctionStore):

```csharp
// Remove if no longer needed:
using Cleipnir.ResilientFunctions.Storage;       // only if InMemoryFunctionStore was the only usage
```

### Gotcha — Initialize() Must Be Awaited Before Build

`PostgreSqlFunctionStore.Initialize()` is async and must complete before `builder.Build()`.
Top-level statements in `Program.cs` support `await` directly. Place the call before
`builder.Build()`. [VERIFIED: package README example]

### Gotcha — PostgreSQL Must Be Running

`Initialize()` will throw if PostgreSQL is not reachable. The `docker-compose.yml` at repo root
provides the dev PostgreSQL instance (`helix-postgres` on port 5432). Add a connection-refused
guard or document that `docker compose up -d` is a prerequisite.

---

## A5 — OPC UA Migration (Remove Opc.UaFx.Advanced)

### Current Situation (VERIFIED: direct codebase read)

- `Agent/Transport/OpcTransport.cs` uses `Opc.UaFx.Client.OpcClient` (line 2 of that file)
- `Agent.csproj` contains `<PackageReference Include="Opc.UaFx.Advanced" Version="2.9.1" />`
- `DeviceAdapterExtensions.cs` line 14 registers:
  `services.AddTransient<IOpcTransport, OpcTransport>();`
- `IOpcTransport` (in `Devices/Transport/IOpcTransport.cs`) has synchronous methods:
  `Connect(string endpoint)`, `Read<T>(nodeId)`, `Write<T>(nodeId, value)`, etc.
- `OpcDeviceTransport` (in `Devices/Transports/Opc/OpcDeviceTransport.cs`) implements
  `IDeviceTransport` with async methods (`ConnectAsync`, `DisconnectAsync`, `SendReceiveAsync`)
- These two interfaces are not compatible — `IOpcTransport` is synchronous, `IDeviceTransport`
  is async

### Resolution

The `IOpcTransport` interface (and the Agent's `OpcTransport.cs`) were authored for the pre-
migration device layer. `OpcDeviceTransport` in the Devices project is the correct target. No
code in the Agent (other than the registration in `DeviceAdapterExtensions.cs`) calls
`IOpcTransport` methods directly — `IOpcTransport` is only used as a DI registration type.

**Approach:**
1. Delete `Helix.Orchestration.Agent/Transport/OpcTransport.cs`
2. Remove `<PackageReference Include="Opc.UaFx.Advanced" Version="2.9.1" />` from `Agent.csproj`
3. In `DeviceAdapterExtensions.cs`, remove the `IOpcTransport` registration line:
   ```csharp
   // REMOVE:
   services.AddTransient<IOpcTransport, OpcTransport>();
   ```
4. Remove the `using Helix.Orchestration.Agent.Transport;` import from `DeviceAdapterExtensions.cs`
   if it becomes unused.

The `Agent.csproj` already has a project reference to Devices, so `OpcDeviceTransport` is
accessible. Future adapter registration (Phase 2) will register `OpcDeviceTransport` as
`IDeviceTransport` via `services.AddHelixOpcAdapter<TAdapter>(opts => { ... })`.

### Gotcha — IOpcTransport May Be Referenced in Tests

```
tests/Helix.Orchestration.Devices.Tests/Transports/OpcTransportTests.cs
```
references `OpcTransportOptions` and `IOpcSession`, but NOT `IOpcTransport` (the old
synchronous interface) or `OpcTransport` (the Opc.UaFx implementation). Test file is safe.
[VERIFIED: grep across tests/]

### Gotcha — AutoAcceptUntrustedCertificates

`OpcDeviceTransport.cs` line 80 sets `AutoAcceptUntrustedCertificates = true` in the
`ApplicationConfiguration`. This is a security concern documented in CONCERNS.md. It is
intentionally deferred to Phase 3 and must not block Phase 1.

---

## A6 — FormatterServices Deprecation Fix

### Finding

`DevicesServiceCollectionExtensions.cs` line 112:

```csharp
var options = (TOptions)FormatterServices.GetUninitializedObject(typeof(TOptions));
```

`FormatterServices.GetUninitializedObject` was deprecated in .NET 7 and **removed** in .NET 8.
The project targets net10.0 so this will not compile once the project builds (currently excluded
from Debug by the .slnx bug). [VERIFIED: direct read of codebase + Microsoft docs]

### Correct Replacement

`RuntimeHelpers.GetUninitializedObject(typeof(TOptions))` is the direct 1:1 replacement,
introduced in .NET 5, available in .NET 10, in the `System.Runtime.CompilerServices` namespace
(no new NuGet package needed — it is part of the BCL). [ASSUMED: based on training knowledge
that RuntimeHelpers.GetUninitializedObject is the successor API in .NET 5+; BCL inclusion
not independently verified via documentation in this session]

```csharp
// FROM:
using System.Runtime.Serialization;   // provides FormatterServices
...
var options = (TOptions)FormatterServices.GetUninitializedObject(typeof(TOptions));

// TO:
using System.Runtime.CompilerServices;  // replace the Serialization using
...
var options = (TOptions)RuntimeHelpers.GetUninitializedObject(typeof(TOptions));
```

### Why NOT Activator.CreateInstance<TOptions>()

`Activator.CreateInstance<T>()` would require a `new()` generic constraint and would call the
parameterless constructor. All `*TransportOptions` types use `required init` properties
(`SerialTransportOptions.PortName`, `OpcTransportOptions.EndpointUrl`, etc.) and have no
explicit parameterless constructor. Setting `init` properties after construction via an
`Action<T>` lambda is **not possible** at compile time. `RuntimeHelpers.GetUninitializedObject`
bypasses constructors entirely (same as `FormatterServices`), making it the behaviorally
identical replacement. Do NOT add the `new()` constraint. [VERIFIED: read of all five
*TransportOptions types — all have `required` + `init` properties]

### Method Called By

`AddHelixSerialAdapter`, `AddHelixTcpAdapter`, `AddHelixModbusAdapter`, `AddHelixOpcAdapter`,
`AddHelixCDllAdapter` — all call `BuildOptions<TOptions>(configureOptions)` internally. These
extension methods are defined but not called from any production code yet (no callers found in
grep scan). The fix ensures the code compiles; correctness of the `Action<T>` pattern is a
pre-existing design concern deferred to Phase 2 when adapters are wired.

---

## A7 — Infrastructure Repositories in SiteServer

### Finding

`SiteServer/Program.cs` never calls `AddHelixInfrastructure`. The `Infrastructure.csproj` IS
referenced by `SiteServer.csproj` (confirmed). `AddHelixInfrastructure` exists in
`InfrastructureServiceExtensions.cs` and registers both `IExperimentRepository` and
`ICampaignRepository` as scoped against their PostgreSQL implementations.
[VERIFIED: direct codebase reads]

### The One-Line Fix

Add to `SiteServer/Program.cs` (after existing service registrations, before `builder.Build()`):

```csharp
builder.Services.AddHelixInfrastructure(builder.Configuration);
```

Add using:

```csharp
using Helix.Orchestration.Infrastructure.Persistence;
```

Add to `SiteServer/appsettings.json`:

```json
"ConnectionStrings": {
  "HelixOrchestration": "Host=localhost;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password"
}
```

### Gotcha — CampaignManager Does Not Use Repositories

`CampaignManager` in SiteServer uses an in-memory `ConcurrentDictionary` internally and does
not inject `ICampaignRepository`. The A7 task makes the repositories available in DI; wiring them
into `CampaignManager` and endpoints is Phase 2 work. For Phase 1, registering them in DI
satisfies the deliverable definition ("live in the DI container").

### Gotcha — AddHelixInfrastructure Throws on Missing Connection String

`AddHelixInfrastructure` throws `InvalidOperationException` if
`ConnectionStrings:HelixOrchestration` is absent. The current `appsettings.json` has no
`ConnectionStrings` section. Add the connection string to `appsettings.json` (or
`appsettings.Development.json` only, to force explicit config in production).

---

## A8 — .slnx Build Configuration Fix

### Finding: The Build Exclusion Syntax (VERIFIED: direct read of Helix.Orchestration.slnx)

In `.slnx` files, `<Build Solution="Debug|*" Project="false" />` is a child element of
`<Project>` that **excludes** the project from the Debug solution configuration (all platforms).
Removing the `<Build ...>` child element enables the project to build in Debug.

The current `.slnx` has 10 projects excluded from Debug builds:

```xml
<!-- Each has this child — REMOVE it to enable building -->
<Build Solution="Debug|*" Project="false" />
```

Projects currently WITHOUT exclusion (building correctly):
- `Helix.Orchestration.Core`
- `Helix.Orchestration.Devices`
- `Helix.Orchestration.SiteServer.Tests`
- `Helix.Orchestration.Core.Tests`

### Target State

Remove all `<Build Solution="Debug|*" Project="false" />` child elements. Convert each affected
`<Project>` element to a self-closing tag:

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

### Gotcha — Enabling All Projects Exposes Hidden Compile Errors

After A8, `dotnet build` will attempt to compile the Agent, Recorder, Infrastructure, and
Simulation projects. Until A5 (OpcTransport.cs delete + package remove) and A6
(FormatterServices fix) are applied, these projects will fail to compile. The recommended
execution order is A8 last (or A8 first with expected failures, then fix A5/A6 to clear them).

---

## Key Decisions

### D1 — IObjectStorageSink Backend: Keep AWSSDK.S3

**Decision:** Keep the existing `MinioObjectStorageSink` with `AWSSDK.S3`. Do NOT switch to the
Minio .NET SDK.

**Rationale:** The AWSSDK.S3 implementation is already correct, tested, and canonical. The
interface is unified and has `WriteAsync`. The "Minio SDK approach" described in the brief
referenced the now-deleted pre-flatten root-level copy. Switching SDKs adds churn with no
functional benefit; Minio 7.0.0 (November 2025 release) has a breaking API change from 6.x that
would require more code change, not less.

### D2 — IOpcTransport Registration: Remove, Not Wrap

**Decision:** Remove the `IOpcTransport` / `OpcTransport` registration from
`DeviceAdapterExtensions.cs`. Do NOT create an `IOpcTransport` → `OpcDeviceTransport` adapter
wrapper.

**Rationale:** No production code calls `IOpcTransport` methods after the registration. The
`IOpcTransport` interface is synchronous; `IDeviceTransport` is async. A wrapper would
perpetuate the synchronous interface that is incompatible with the Devices layer's design. Phase 2
will register `OpcDeviceTransport` via `services.AddHelixOpcAdapter<TAdapter>(...)`.

### D3 — A6 Replacement: RuntimeHelpers, Not Activator

**Decision:** Use `RuntimeHelpers.GetUninitializedObject(typeof(TOptions))` from
`System.Runtime.CompilerServices`, not `Activator.CreateInstance<TOptions>()`.

**Rationale:** All `*TransportOptions` types use `required init` properties, which cannot be set
via `Action<T>` lambda after construction. `RuntimeHelpers.GetUninitializedObject` bypasses
constructors entirely, preserving the existing behavior while removing the deprecated API.

### D4 — Cleipnir Version Upgrade Required

**Decision:** Bump `Cleipnir.ResilientFunctions` from `4.0.0` to `4.*` in `Core.csproj` and
`Agent.csproj` as part of A4.

**Rationale:** `Cleipnir.ResilientFunctions.PostgreSQL 4.2.5` has a hard NuGet dependency on
`Cleipnir.ResilientFunctions >= 4.2.5`. Without the bump, restore will fail. The API is
backward-compatible; `FunctionsRegistry` constructor signature is unchanged between versions.
[VERIFIED: nuspec from api.nuget.org; README from both 4.0.0 and 4.2.5 cached packages]

---

## Package Legitimacy Audit

> Note: slopcheck runs against PyPI; .NET packages are on NuGet. All results below were
> verified against nuget.org via `dotnet package search` and direct nuspec inspection.

| Package | Registry | Owner | Downloads | slopcheck | Disposition |
|---------|----------|-------|-----------|-----------|-------------|
| `Cleipnir.ResilientFunctions` | nuget.org | stidsborg | 35,697 | N/A (PyPI SLOP — is .NET pkg) | Approved [VERIFIED: nuget.org] |
| `Cleipnir.ResilientFunctions.PostgreSQL` | nuget.org | stidsborg | 17,123 | N/A (PyPI SLOP — is .NET pkg) | Approved [VERIFIED: nuget.org] |
| `Minio` (existing) | nuget.org | minio | 27M | OK on PyPI (different ecosystem pkg) | Not used — no change |
| `AWSSDK.S3` (existing) | nuget.org | awsdotnet | 575M | N/A | Approved — keep existing |
| `Opc.UaFx.Advanced` (to remove) | commercial | N/A | N/A | N/A | REMOVE — replaced by OPCFoundation pkg already in Devices |

**slopcheck ran against PyPI (Python registry) — .NET packages do not appear there.**
Verification used `dotnet package search` against nuget.org and `curl` against
`api.nuget.org/v3-flatcontainer` for dependency inspection.

---

## Risks

### R1 — PostgreSQL Unavailable at Agent Startup (HIGH, but scoped)

`PostgreSqlFunctionStore.Initialize()` makes a live PostgreSQL connection. If PostgreSQL is
not running, the Agent fails to start. This is the desired behavior (fail-fast on misconfiguration)
but developers must have `docker compose up -d` running. Document this as a startup prerequisite.

**Mitigation:** The docker-compose.yml healthcheck on the postgres service means `docker compose
up -d` blocks until PostgreSQL is ready. No code change needed for Phase 1.

### R2 — Cleipnir 4.0.0 → 4.2.5 Version Bump (LOW risk)

The base package API is backward-compatible. No call sites change. The risk is that 4.2.5 targets
`net9.0` (confirmed from nuspec), not `net10.0`. .NET 10 can load net9.0 assemblies (forward
compatibility is standard .NET policy). This is the same situation as several other packages in
the solution. [ASSUMED: .NET 10 forward-loads net9.0 assemblies — standard .NET behavior,
not independently verified for Cleipnir specifically]

### R3 — A6 Init Property Semantics (LOW risk)

The `BuildOptions<TOptions>` method creates an uninitialized object and then calls
`configureOptions(options)` — an `Action<TOptions>`. Since no call sites exist yet,
`RuntimeHelpers.GetUninitializedObject` is safe. Future callers MUST use regular property
setters (`opts.Host = ...`), not object initializer syntax, since the object is already
constructed. If a future caller tries `init`-only properties via the lambda, it will produce a
compiler error at that call site — the fix is to change `init` to `set` in the affected options
class.

### R4 — Minio console port 9002 / API port 9000 (LOW, operational)

The docker-compose.yml exposes MinIO on port 9000 (API) and 9002 (console). The
`MinioOptions.ServiceUrl` in appsettings must use port 9000. This is already correct in the
`MinioOptions` defaults (`ServiceUrl = string.Empty` — must be set in appsettings).

---

## Execution Order (Recommended)

| Step | Deliverable | Files Changed | Why This Order |
|------|-------------|---------------|----------------|
| 1 | A6 | `DevicesServiceCollectionExtensions.cs` | One-line fix; unblocks compile |
| 2 | A5 | `OpcTransport.cs` (delete), `Agent.csproj`, `DeviceAdapterExtensions.cs` | Removes commercial package; unblocks compile |
| 3 | A8 | `Helix.Orchestration.slnx` | Enable all projects; verify A5/A6 cleared compile errors |
| 4 | A1 | `Worker.cs` (delete) | Trivial; clears dead code |
| 5 | A2 | grep verification only | Confirm no diverged copy |
| 6 | A3 | `DeviceAdapterExtensions.cs` | Add log line to factory |
| 7 | A4 | `Agent.csproj`, `Core.csproj`, `Agent/Program.cs`, `Agent/appsettings.json` | Requires A5 (package remove) first to avoid conflicts; add PostgreSQL store |
| 8 | A7 | `SiteServer/Program.cs`, `SiteServer/appsettings.json` | Independent; add DI registration |

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.x |
| Config file | per-project `.csproj` (no global xunit.runner.json) |
| Quick run command | `dotnet test tests/Helix.Orchestration.Devices.Tests/ -x` |
| Full suite command | `dotnet test --no-build` (after `dotnet build`) |

### Phase Requirements → Test Map

| Deliverable | Behavior | Test Type | Command |
|-------------|----------|-----------|---------|
| A1 — Recorder merge | Worker.cs deleted; Program.cs compiles with real services | build smoke | `dotnet build Helix.Orchestration.Recorder/` |
| A2 — IObjectStorageSink | Interface unified; one canonical file | grep check | `find . -name "IObjectStorageSink.cs" \| wc -l` → 1 |
| A3 — CapabilityRegistry | Registry initializes; log line visible at startup | build smoke | `dotnet build Helix.Orchestration.Agent/` |
| A4 — Cleipnir PostgreSQL | FunctionsRegistry uses PostgreSqlFunctionStore; Initialize() called | integration (requires Postgres) | `dotnet run --project Helix.Orchestration.Agent/` (starts without exception) |
| A5 — OPC UA migration | No Opc.UaFx reference in solution | grep check | `grep -r "Opc.UaFx" . --include="*.csproj"` → empty |
| A6 — FormatterServices | Code compiles; no deprecated API | build smoke | `dotnet build Helix.Orchestration.Devices/` |
| A7 — SiteServer repos | IExperimentRepository/ICampaignRepository resolve in DI | integration | `dotnet run --project Helix.Orchestration.SiteServer/` (starts without DI exception) |
| A8 — .slnx config | All projects build in Debug configuration | build | `dotnet build Helix.Orchestration.slnx` |

### Wave 0 Gaps

No new test files are needed for Phase 1. All deliverables are compile-time fixes or DI
registrations verifiable via build + startup smoke test. The existing test suite in
`tests/Helix.Orchestration.Devices.Tests/` and `tests/Helix.Orchestration.Core.Tests/`
should continue to pass (they are currently building).

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `RuntimeHelpers.GetUninitializedObject` is the correct BCL replacement for `FormatterServices.GetUninitializedObject` in .NET 5+ | A6 | Could use wrong namespace/method; fallback is `(TOptions)Activator.CreateInstance(typeof(TOptions))` but this calls the ctor, which may behave differently for types without parameterless constructors |
| A2 | .NET 10 can load `net9.0` target-framework assemblies (forward compatibility) for Cleipnir 4.2.5 | A4 (version bump) | Build failure; would require waiting for Cleipnir to publish a net10.0 target |

---

## Sources

### Primary (HIGH confidence)
- Direct codebase reads: all `.cs` and `.csproj` files referenced above (verified in this session)
- `api.nuget.org/v3-flatcontainer/cleipnir.resilientfunctions.postgresql/4.2.5/*.nuspec` — dependency version confirmation
- `~/.nuget/packages/cleipnir.resilientfunctions/{4.0.0,4.2.5}/README.md` — FunctionsRegistry + PostgreSqlFunctionStore API
- `~/.nuget/packages/minio/6.0.5/readme.md` — Minio 6.x PutObjectArgs builder pattern
- `github.com/stidsborg/Cleipnir.ResilientFunctions` — PostgreSqlFunctionStore class definition, constructor, Initialize()
- `github.com/minio/minio-dotnet` — IMinioClient interface (v7.0.0 API)
- `dotnet package search` against nuget.org — package existence and version confirmation

### Secondary (MEDIUM confidence)
- `github.com/minio/minio-dotnet/blob/6.0.5/README.md` — Minio 6.x PutObjectArgs confirmation
- `git show 53a0eeb` — confirmed pre-flatten state had two IObjectStorageSink files

### Tertiary (LOW confidence — marked [ASSUMED])
- RuntimeHelpers.GetUninitializedObject availability (.NET 5+) — training knowledge, not verified via official docs in this session
- .NET 10 forward-loads net9.0 assemblies — standard .NET behavior, not specifically confirmed for Cleipnir

---

## Metadata

**Confidence breakdown:**
- A1 (delete Worker.cs): HIGH — confirmed Program.cs does not register it
- A2 (IObjectStorageSink): HIGH — one canonical file confirmed by find + git history
- A3 (CapabilityRegistry placeholder): HIGH — empty registry confirmed by direct read
- A4 (Cleipnir PostgreSQL API): HIGH — API confirmed from package README + nuspec dependency
- A4 (version bump): HIGH — hard dependency confirmed from nuspec
- A5 (OPC UA): HIGH — one reference confirmed; interface mismatch confirmed; no hidden callers found
- A6 (FormatterServices): MEDIUM — RuntimeHelpers is [ASSUMED]; AWSSDK init-property analysis is HIGH
- A7 (SiteServer): HIGH — AddHelixInfrastructure exists; SiteServer has project reference; no call exists
- A8 (.slnx): HIGH — syntax confirmed by direct read of existing file; semantics match expected behavior

**Research date:** 2026-05-26
**Valid until:** 2026-06-26 (stable .NET ecosystem; Cleipnir is slow-moving)

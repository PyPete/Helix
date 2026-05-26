# Phase 1: Technical Debt Resolution — PLAN

**Phase:** 1 — Technical Debt Resolution
**Requirements covered:** A1, A2, A3, A4, A5, A6, A7, A8
**Status:** Ready to execute
**Checker:** PASS (post-fix)

---

## Overview

Eight targeted fixes that collectively make the solution build cleanly in all configurations,
eliminate dead or broken code, and wire the DI container correctly. No new architectural patterns
are introduced. All changes are surgical: delete dead files, swap package references, add missing
registrations, and correct build exclusions.

### Execution order rationale

The dependency chain runs: A6 (FormatterServices fix) and A5 (OPC UA removal) unblock the build
first; A8 (.slnx fix) then reveals the clean state; A1/A2/A3 are independent housekeeping; A4
(Cleipnir) depends on A5 completing to ensure the Agent.csproj is clean before adding packages;
A7 (SiteServer DI) is independent. The docker-compose deliverable is pre-satisfied — the root
`docker-compose.yml` already contains all three services and the internal duplicate was removed
in the flatten commit.

---

## Wave Structure

| Wave | Tasks | Parallel? | Notes |
|------|-------|-----------|-------|
| 1 | T1 (A6 FormatterServices) | — | One-liner; unblocks Devices project compile |
| 2 | T2 (A5 OPC UA removal), T3 (A8 .slnx fix) | Sequential (T2 then T3) | T3 must run after T2 completes — T3's `dotnet build` acceptance test requires T2's OPC UA compile error to be cleared first |
| 3 | T4 (A1 delete Worker.cs), T5 (A2 verify IObjectStorageSink), T6 (A3 CapabilityRegistry) | Yes | All independent housekeeping |
| 4 | T7 (A4 Cleipnir PostgreSQL), T8 (A7 SiteServer DI) | Yes | Independent of each other |
| 5 | T9 (docker-compose verification) | — | Confirm root file already complete; no code change |

---

## Tasks

---

### T1 — A6: Replace `FormatterServices.GetUninitializedObject` (Wave 1)

**Requirement:** A6
**Files changed:**
- `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs`

**Change:**

In `DevicesServiceCollectionExtensions.cs`:

1. Replace the `using` directive on line 1:
   - Remove: `using System.Runtime.Serialization;`
   - No new `using` directive is needed — `RuntimeHelpers` is accessible via implicit usings
     in this `Microsoft.NET.Sdk` project (`System.Runtime.CompilerServices` is already in scope).

2. Replace line 112 inside `BuildOptions<TOptions>`:
   - Remove: `var options = (TOptions)FormatterServices.GetUninitializedObject(typeof(TOptions));`
   - Add: `var options = (TOptions)RuntimeHelpers.GetUninitializedObject(typeof(TOptions));`

   Do NOT add a `new()` generic constraint to `BuildOptions<TOptions>`. All five
   `*TransportOptions` types (`SerialTransportOptions`, `TcpTransportOptions`,
   `ModbusTransportOptions`, `OpcTransportOptions`, `CDllTransportOptions`) use `required init`
   properties and have no explicit parameterless constructor. `RuntimeHelpers.GetUninitializedObject`
   bypasses constructors entirely — this is the behaviorally identical replacement for
   `FormatterServices.GetUninitializedObject` on .NET 5+. Using `Activator.CreateInstance<T>()`
   would require a `new()` constraint and would call constructors that do not exist on these types.

3. The `BuildOptions` method signature and body remain otherwise identical:
   ```
   private static TOptions BuildOptions<TOptions>(Action<TOptions> configureOptions)
       where TOptions : class
   {
       var options = (TOptions)RuntimeHelpers.GetUninitializedObject(typeof(TOptions));
       configureOptions(options);
       return options;
   }
   ```

**Context:** `FormatterServices.GetUninitializedObject` was removed in .NET 8.
`RuntimeHelpers.GetUninitializedObject` is the direct BCL successor in `System.Runtime.CompilerServices`
(no new NuGet package required). The Devices project is currently excluded from Debug builds by
the .slnx bug; this fix ensures it compiles clean once A8 enables it.

**Acceptance test:**
```
dotnet build Helix.Orchestration.Devices/Helix.Orchestration.Devices.csproj
```
Must exit 0 with no warnings about `FormatterServices` or `System.Runtime.Serialization`.

**Verify grep:**
```
grep -r "FormatterServices" Helix.Orchestration.Devices/ --include="*.cs"
```
Must return no matches.

**Dependencies:** None

---

### T2 — A5: Remove `Opc.UaFx.Advanced` and delete `OpcTransport.cs` (Wave 2)

**Requirement:** A5
**Files changed:**
- `Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj` — remove PackageReference
- `Helix.Orchestration.Agent/Transport/OpcTransport.cs` — DELETE file
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` — remove registration

**Change:**

1. In `Helix.Orchestration.Agent.csproj`, remove this line from the `<ItemGroup>` of package references:
   ```xml
   <PackageReference Include="Opc.UaFx.Advanced" Version="2.9.1" />
   ```

2. Delete the file `Helix.Orchestration.Agent/Transport/OpcTransport.cs` entirely.
   This file implements `IOpcTransport` using `Opc.UaFx.Client.OpcClient` and has no callers in
   production code other than the DI registration in `DeviceAdapterExtensions.cs`.

3. In `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`:
   - Remove only: `services.AddTransient<IOpcTransport, OpcTransport>();`
   - **Keep** `using Helix.Orchestration.Agent.Transport;` — `SerialTransport`, `ModbusTransport`,
     and `TcpTransport` all live in `Helix.Orchestration.Agent.Transport` and must continue to
     resolve. Do NOT remove this using directive.

   After the change, `DeviceAdapterExtensions.cs` should look like:
   ```
   using Helix.Orchestration.Agent.Transport;
   using Helix.Orchestration.Core.Capabilities;
   using Helix.Orchestration.Devices.Transport;
   using Microsoft.Extensions.DependencyInjection;

   namespace Helix.Orchestration.Agent.Adapters.Devices;

   public static class DeviceAdapterExtensions
   {
       public static IServiceCollection AddMigratedDeviceAdapters(this IServiceCollection services)
       {
           services.AddTransient<ISerialTransport, SerialTransport>();
           services.AddTransient<IModbusTransport, ModbusTransport>();
           services.AddTransient<ITcpTransport, TcpTransport>();
           // IOpcTransport/OpcTransport removed — was Opc.UaFx.Advanced (commercial, now deleted)
           // Phase 2 will register OpcDeviceTransport via services.AddHelixOpcAdapter<TAdapter>(...)

           services.AddSingleton<CapabilityRegistry>(sp =>
           {
               var registry = new CapabilityRegistry();
               return registry;
           });

           services.AddSingleton<ICapabilityInvoker>(sp => sp.GetRequiredService<CapabilityRegistry>());

           return services;
       }
   }
   ```

**Context:** `IOpcTransport` is a synchronous interface from the pre-migration device layer;
`IDeviceTransport` (used by `OpcDeviceTransport`) is the async interface in the Devices project.
These are incompatible. No production code calls `IOpcTransport` methods after the DI registration
— it is only used as a registration type. The Devices project already contains `OpcDeviceTransport`
(OPC Foundation open-source library), which Phase 2 will wire into the CapabilityRegistry.
Do NOT create an adapter wrapper from `IOpcTransport` to `OpcDeviceTransport`.

**Acceptance test:**
```
dotnet build Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj
grep -r "Opc.UaFx" . --include="*.csproj"
```
Build must exit 0. Grep must return no matches.

**Verify file deleted:**
```
test -f Helix.Orchestration.Agent/Transport/OpcTransport.cs && echo "FAIL: file still exists" || echo "OK"
```

**Dependencies:** None (independent of T1, can run in parallel)

---

### T3 — A8: Fix `.slnx` build configuration (Wave 2)

**Requirement:** A8
**Files changed:**
- `Helix.Orchestration.slnx`

**Change:**

Replace the entire contents of `Helix.Orchestration.slnx` with the following. The only change
is removing all `<Build Solution="Debug|*" Project="false" />` child elements so every project
builds in Debug configuration:

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

Ten `<Build Solution="Debug|*" Project="false" />` child elements are removed. The projects
previously building without restriction (`Core`, `Devices`, `SiteServer.Tests`, `Core.Tests`)
retain the same self-closing `<Project ... />` form.

**Context:** T3 should be committed after T1 and T2 are complete. Once the .slnx has no
exclusions, `dotnet build` will compile all projects and compile errors previously hidden will
surface. T1 (FormatterServices) and T2 (OpcTransport.cs / Opc.UaFx.Advanced) eliminate the two
known compile errors in the previously excluded projects.

**Acceptance test:**
```
dotnet build Helix.Orchestration.slnx -c Debug
```
Must exit 0 with all 14 projects compiled (no skips). If any compile errors appear beyond
the two addressed by T1/T2, record them before proceeding.

**Verify no exclusions remain:**
```
grep -c "Build Solution" Helix.Orchestration.slnx
```
Must return `0`.

**Dependencies:** T1, T2 should be applied first (to clear the compile errors their projects had)

---

### T4 — A1: Delete `Worker.cs` dead code (Wave 3)

**Requirement:** A1
**Files changed:**
- `Helix.Orchestration.Recorder/Worker.cs` — DELETE file

**Change:**

Delete `Helix.Orchestration.Recorder/Worker.cs` entirely.

`Worker` is never registered with `AddHostedService<Worker>()` in `Program.cs`.
`Program.cs` (lines 37–38) already registers the real pipeline:
```
builder.Services.AddHostedService<MqttRecorderService>();
builder.Services.AddHostedService<RecorderWriterService>();
```
`Worker.cs` is a 30-second delay loop placeholder that was never wired. Deleting it has no
runtime effect; the real services are already active.

After deletion, verify no stray reference remains:
```
grep -r "Worker" Helix.Orchestration.Recorder/ --include="*.cs"
```
Expected output: none (or only namespace/logging text, not `AddHostedService<Worker>` or `new Worker`).

**Acceptance test:**
```
dotnet build Helix.Orchestration.Recorder/Helix.Orchestration.Recorder.csproj
```
Must exit 0.

**Dependencies:** None (independent; can run with T5, T6 in parallel)

---

### T5 — A2: Verify `IObjectStorageSink` unification (Wave 3)

**Requirement:** A2
**Files changed:** None (verification only)

**Change:**

This is a verification step. Confirm that after the flatten commit (53a0eeb), only one
`IObjectStorageSink.cs` file exists in the repository and it is the canonical Infrastructure version.

Run:
```
find . -name "IObjectStorageSink.cs" -not -path "*/bin/*" -not -path "*/obj/*"
```
Expected: exactly one result:
`./Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs`

If a second file is found outside `Helix.Orchestration.Infrastructure/`:
- Confirm it is not referenced by any `.csproj` in the solution (using `grep`)
- Delete the unreferenced duplicate

The canonical interface at `Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs`
already contains both the full five-method API and `WriteAsync(key, stream, contentType, ct)`.
The `MinioObjectStorageSink` implements it using `AWSSDK.S3`. No interface or implementation
changes are needed. Do NOT switch to the Minio .NET SDK — the AWSSDK.S3 implementation is
correct and canonical (per research decision D1).

**Acceptance test:**
```
find . -name "IObjectStorageSink.cs" -not -path "*/bin/*" -not -path "*/obj/*" | wc -l
```
Must output `1`.

**Dependencies:** None (independent)

---

### T6 — A3: Populate `CapabilityRegistry` with diagnostic log (Wave 3)

**Requirement:** A3
**Files changed:**
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`

**Change:**

In `DeviceAdapterExtensions.cs`, update the `CapabilityRegistry` singleton factory to inject
`ILogger<CapabilityRegistry>` and emit a startup diagnostic. This proves the wiring compiles,
that the DI factory executes at startup, and that log observability works — all without registering
any real adapters (which require Phase 2 drivers).

Replace the current empty factory (lines 17–21):
```csharp
services.AddSingleton<CapabilityRegistry>(sp =>
{
    var registry = new CapabilityRegistry();

    return registry;
});
```

With:
```csharp
services.AddSingleton<CapabilityRegistry>(sp =>
{
    var registry = new CapabilityRegistry();
    var logger = sp.GetRequiredService<ILogger<CapabilityRegistry>>();
    logger.LogInformation(
        "CapabilityRegistry initialized — 0 adapters registered (Phase 1 placeholder). " +
        "Phase 2 will register real device adapters.");
    return registry;
});
```

Add the required `using` directive if not already present (check current imports):
```csharp
using Microsoft.Extensions.Logging;
```

No other changes. Do not register any `DeviceCapabilityAdapter` instances — no concrete device
adapters exist in the solution yet (they are Phase 2 work). The log line at startup satisfies
the Phase 1 deliverable: "capability invocations no longer dispatch into a void" is demonstrated
by the registry initializing correctly and being observable via logs.

**Note:** T2 keeps `using Helix.Orchestration.Agent.Transport;` in the file (SerialTransport/
ModbusTransport/TcpTransport depend on it). Confirm the file state matches the output described in T2
before editing.

**Scope note:** CONCERNS.md Recommended Action item 3 calls for re-implementing a device driver.
Per the ROADMAP Phase 1 deliverable ("even with placeholder registrations demonstrating the wiring"),
this item is resolved by the startup diagnostic log. Full VapourtecR4 driver implementation is
Phase 2 scope — this task closes the CONCERNS item for Phase 1 purposes.

**Acceptance test:**
```
dotnet build Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj
```
Must exit 0. When the Agent is started (`dotnet run --project Helix.Orchestration.Agent/`),
the log line `CapabilityRegistry initialized — 0 adapters registered` must appear in console
output during startup.

**Dependencies:** T2 (modifies the same file — apply T2 first to get the clean state)

---

### T7 — A4: Replace `InMemoryFunctionStore` with Cleipnir PostgreSQL store (Wave 4)

**Requirement:** A4
**Files changed:**
- `Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj` — add package, bump version
- `Helix.Orchestration.Core/Helix.Orchestration.Core.csproj` — bump Cleipnir base version
- `Helix.Orchestration.Agent/Program.cs` — replace store initialization + registration
- `Helix.Orchestration.Agent/appsettings.json` — add ConnectionStrings section

**Change (step by step):**

**Step 1 — Update `Helix.Orchestration.Core.csproj`:**

Change the Cleipnir base package version from `4.0.0` to `4.*` (allows NuGet to resolve 4.2.5+):
```xml
<!-- FROM: -->
<PackageReference Include="Cleipnir.ResilientFunctions" Version="4.0.0" />
<!-- TO: -->
<PackageReference Include="Cleipnir.ResilientFunctions" Version="4.*" />
```

**Step 2 — Update `Helix.Orchestration.Agent.csproj`:**

a) Change the Cleipnir base package version from `4.0.0` to `4.*`:
```xml
<!-- FROM: -->
<PackageReference Include="Cleipnir.ResilientFunctions" Version="4.0.0" />
<!-- TO: -->
<PackageReference Include="Cleipnir.ResilientFunctions" Version="4.*" />
```

b) Add the PostgreSQL extension package:
```xml
<PackageReference Include="Cleipnir.ResilientFunctions.PostgreSQL" Version="4.*" />
```

**Step 3 — Update `Helix.Orchestration.Agent/appsettings.json`:**

Add a `ConnectionStrings` section. The existing file has `Logging`, `AllowedHosts`, and `Mqtt`
sections; add `ConnectionStrings` at the same level:
```json
"ConnectionStrings": {
  "HelixOrchestration": "Host=localhost;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password"
}
```
This matches the PostgreSQL service in `docker-compose.yml` (`POSTGRES_DB: helix_orchestration`,
`POSTGRES_USER: helix`, `POSTGRES_PASSWORD: helix_dev_password`, port 5432).

**Step 4 — Update `Helix.Orchestration.Agent/Program.cs`:**

a) Add usings after the existing `using` block:
```csharp
using Cleipnir.ResilientFunctions.PostgreSQL;   // PostgreSqlFunctionStore
```
The `using Cleipnir.ResilientFunctions.Storage;` line is already present (line 2) — keep it,
as `IFunctionStore` is defined there.

b) Find the durable workflow execution block (current lines 56–58):
```csharp
// Durable workflow execution
builder.Services.AddSingleton(new InMemoryFunctionStore());
builder.Services.AddSingleton<FunctionsRegistry>(sp =>
    new FunctionsRegistry(sp.GetRequiredService<InMemoryFunctionStore>()));
```

Replace with:
```csharp
// Durable workflow execution — PostgreSQL-backed (survives process restart)
var connectionString = builder.Configuration.GetConnectionString("HelixOrchestration")
    ?? throw new InvalidOperationException(
        "Connection string 'HelixOrchestration' is required for Cleipnir PostgreSQL store. " +
        "Ensure docker compose is running: docker compose up -d");

var postgresFunctionStore = new PostgreSqlFunctionStore(connectionString);
await postgresFunctionStore.Initialize();   // Creates Cleipnir tables idempotently; must complete before Build()

builder.Services.AddSingleton<IFunctionStore>(postgresFunctionStore);
builder.Services.AddSingleton<FunctionsRegistry>(sp =>
    new FunctionsRegistry(sp.GetRequiredService<IFunctionStore>()));
```

This block must appear before `var app = builder.Build();` (currently line 88). The
top-level-statements `Program.cs` supports `await` directly.

c) Remove `InMemoryFunctionStore` from DI — after the replacement above, there is no longer a
`builder.Services.AddSingleton(new InMemoryFunctionStore())` call. Confirm no other reference to
`InMemoryFunctionStore` exists in `Program.cs`.

**Context:** `PostgreSqlFunctionStore.Initialize()` creates the Cleipnir tables if they do not
exist (idempotent). It makes a live connection to PostgreSQL. If PostgreSQL is not running,
`Initialize()` throws `NpgsqlException`. The prerequisite for running the Agent is
`docker compose up -d` at the repo root. The `docker-compose.yml` PostgreSQL service has a
healthcheck; `docker compose up -d` will not return until Postgres is ready.

**Acceptance test:**
```
dotnet build Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj
```
Must exit 0. Then with `docker compose up -d` running:
```
dotnet run --project Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj
```
Must start without exception. Cleipnir tables (`RFunctions`, etc.) must be visible in the
`helix_orchestration` database. If using psql:
```
docker exec helix-postgres psql -U helix -d helix_orchestration -c "\dt"
```
Must list at least one Cleipnir table.

**Dependencies:** T2 (Agent.csproj must be clean of Opc.UaFx before adding new packages to avoid
restore conflicts). Can run in parallel with T8.

---

### T8 — A7: Register `AddHelixInfrastructure` in SiteServer (Wave 4)

**Requirement:** A7
**Files changed:**
- `Helix.Orchestration.SiteServer/Program.cs` — add DI registration call
- `Helix.Orchestration.SiteServer/appsettings.Development.json` — add connection string

**Change:**

**Step 1 — Update `Helix.Orchestration.SiteServer/Program.cs`:**

Add the following `using` directive to the top of the file (after existing usings):
```csharp
using Helix.Orchestration.Infrastructure.Persistence;
```

Add the registration call after the existing service registrations and before `var app = builder.Build()`:
```csharp
// Register PostgreSQL repositories for campaigns and experiments (Infrastructure project)
builder.Services.AddHelixInfrastructure(builder.Configuration);
```

Place it immediately before the line `var app = builder.Build();` (currently line 35).

`AddHelixInfrastructure` registers:
- `DbConnectionFactory` as Singleton (wraps the connection string)
- `IExperimentRepository` → `PostgresExperimentRepository` as Scoped
- `ICampaignRepository` → `PostgresCampaignRepository` as Scoped

The `SiteServer.csproj` already has a `ProjectReference` to `Helix.Orchestration.Infrastructure`
so the `AddHelixInfrastructure` extension method is already available.

**Step 2 — Update `Helix.Orchestration.SiteServer/appsettings.Development.json`:**

Add a `ConnectionStrings` section (the existing file only has `Logging`):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Helix.Orchestration.Simulation": "Trace",
      "Helix.Orchestration.SiteServer.Mqtt": "Debug"
    }
  },
  "ConnectionStrings": {
    "HelixOrchestration": "Host=localhost;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password"
  }
}
```

Use `appsettings.Development.json` (not `appsettings.json`) so that production deployments
require explicit connection string configuration rather than inheriting a dev default.
`AddHelixInfrastructure` throws `InvalidOperationException` if `ConnectionStrings:HelixOrchestration`
is absent — using the Development override makes this a fail-fast misconfiguration error in
non-dev environments.

**Note on scope:** `AddHelixInfrastructure` makes the repositories available in the DI container.
`CampaignManager` (SiteServer) currently uses an in-memory `ConcurrentDictionary` internally and
does not inject `ICampaignRepository`. Wiring the repositories into `CampaignManager` and the
endpoint handlers is Phase 2 work. The Phase 1 deliverable is that `IExperimentRepository` and
`ICampaignRepository` resolve correctly from DI — not that they are used by all code paths.

**Acceptance test:**
```
dotnet build Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj
```
Must exit 0. With docker compose running:
```
dotnet run --project Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj
```
Must start without DI exception. The startup log must not contain
`InvalidOperationException: Unable to resolve service for type 'IExperimentRepository'`.

**Dependencies:** None (independent of T7, can run in parallel)

---

### T9 — Docker-compose consolidation verification (Wave 5)

**Requirement:** ROADMAP.md deliverable "docker-compose.yml (root) consolidated"
**Files changed:** None if already complete; `Helix.Orchestration/docker-compose.yml` DELETE if still present

**Change:**

The root `docker-compose.yml` already contains all three required services:
- `mosquitto` (eclipse-mosquitto:2, ports 1883 + 9001)
- `postgres` (postgres:16, port 5432, with `helix_orchestration` DB, healthcheck, init scripts)
- `minio` (minio/minio:latest, ports 9000 + 9002, healthcheck)

The `Helix.Orchestration/docker-compose.yml` internal duplicate was removed in the flatten
commit (53a0eeb). Verify this is still the case:

```
find . -name "docker-compose.yml" -not -path "*/bin/*" -not -path "*/obj/*"
```

Expected: exactly one result — `./docker-compose.yml`.

If a second `docker-compose.yml` is found inside a subdirectory, inspect whether it is still
referenced by anything in the solution, then delete it.

No code changes needed. The deliverable is satisfied.

**Acceptance test:**
```
find . -name "docker-compose.yml" -not -path "*/bin/*" | wc -l
```
Must output `1`.

```
docker compose config --quiet
```
Must exit 0 (validates YAML syntax of root docker-compose.yml).

**Dependencies:** None

---

## Risks

### R1 — PostgreSQL unavailable at Agent startup (HIGH impact, mitigated by process)

`PostgreSqlFunctionStore.Initialize()` (T7) throws `NpgsqlException` if PostgreSQL is not
reachable. This is fail-fast behavior — the Agent refuses to start misconfigured.

**Mitigation:** Always run `docker compose up -d` before starting the Agent locally. The
`docker-compose.yml` PostgreSQL service has a `pg_isready` healthcheck; `docker compose up -d`
blocks until the service is healthy. Document this as the single dev prerequisite.

### R2 — Cleipnir 4.0.0 → 4.2.5 version bump (LOW risk)

The base `Cleipnir.ResilientFunctions` package is bumped from 4.0.0 to 4.* (resolves to 4.2.5).
The package targets `net9.0`. .NET 10 loads net9.0 assemblies under standard forward-compatibility
rules. The `FunctionsRegistry` constructor signature is unchanged between versions.

**Fallback:** If NuGet restore fails with a TFM compatibility warning that blocks compilation,
pin explicitly to `Version="4.2.5"` instead of `4.*`.

### R3 — T6 `ILogger<CapabilityRegistry>` resolution (LOW risk)

The `ILogger<CapabilityRegistry>` resolution inside the singleton factory lambda requires that
`ILogger` is registered in the DI container before `CapabilityRegistry` is first resolved.
In a standard `WebApplication.CreateBuilder` host, Serilog is registered via `UseSerilog` before
any scoped services are resolved, so this is safe.

**Fallback:** If DI throws `InvalidOperationException: Unable to resolve ILogger<CapabilityRegistry>`,
wrap in a null-safe pattern:
```csharp
var logger = sp.GetService<ILogger<CapabilityRegistry>>();
logger?.LogInformation("CapabilityRegistry initialized...");
```

### R4 — A6 `init`-only property semantics (LOW risk, future)

`RuntimeHelpers.GetUninitializedObject` bypasses constructors. Future callers of
`AddHelixSerialAdapter<TAdapter>` etc. must use regular property setters in the `configureOptions`
lambda (e.g., `opts.PortName = "/dev/ttyUSB0"`), not C# object initializer syntax — `init`-only
properties cannot be set after construction. This is a Phase 2 concern (no callers exist yet).

### R5 — .slnx enables previously hidden compile errors (LOW risk, expected)

After T3 (A8) enables all projects in Debug, `dotnet build` may reveal compile errors in
projects previously excluded. T1 and T2 address the two known errors (FormatterServices and
OpcTransport). If additional unknown errors appear, they must be diagnosed before Phase 1 can
be declared complete.

---

## Threat Model

### Trust Boundaries

| Boundary | Description |
|----------|-------------|
| Agent ↔ PostgreSQL | Agent's `PostgreSqlFunctionStore` connects to Postgres over TCP. In dev, no TLS; credentials in `appsettings.json`. |
| SiteServer ↔ PostgreSQL | SiteServer's `AddHelixInfrastructure` connects to Postgres over TCP. Dev credentials in `appsettings.Development.json`. |
| Agent ↔ MQTT broker | Agent's `MqttTrendIngestionService` connects to Mosquitto with no authentication. |

### STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation |
|-----------|----------|-----------|-------------|------------|
| T1-01 | Information Disclosure | `appsettings.json` / `appsettings.Development.json` — dev PostgreSQL password `helix_dev_password` committed to source | accept | Dev-only credential; matches docker-compose defaults. Production hardening (TLS, credential rotation) is Phase 10 scope. |
| T1-02 | Spoofing | MQTT broker — anonymous access, no TLS | accept | Dev-only posture; broker is localhost-bound in docker-compose. Production TLS is Phase 10 scope. |
| T1-03 | Tampering | OPC UA — `OpcDeviceTransport.cs` has `AutoAcceptUntrustedCertificates = true` | accept | Known concern (CONCERNS.md). Not introduced by Phase 1 changes. Remediation deferred to Phase 3 (SystagOpcUaClient). |
| T1-04 | Elevation of Privilege | `RuntimeHelpers.GetUninitializedObject` bypasses constructors — could construct objects in unexpected state | accept | Exact behavioral replacement for `FormatterServices.GetUninitializedObject`. No new attack surface; callers already controlled this pattern. |
| T1-SC | Tampering | NuGet package install — `Cleipnir.ResilientFunctions.PostgreSQL 4.2.5` | mitigate | Package verified against nuget.org (owner: stidsborg, 17,123 downloads). Run `dotnet restore` with integrity check; review restore output for unexpected transitive packages. |

---

## Success Criteria

All criteria from ROADMAP.md Phase 1:

- [ ] `dotnet build Helix.Orchestration.slnx` exits 0 in Debug configuration with all projects included
- [ ] `dotnet run --project Helix.Orchestration.Recorder/` starts without exception and logs MQTT connection attempts (not a 30-second delay loop)
- [ ] `dotnet run --project Helix.Orchestration.Agent/` starts without exception; CapabilityRegistry logs at least one registered entry (or explicit diagnostic, not a silent void)
- [ ] Agent process can be stopped and restarted; Cleipnir confirms the PostgreSQL store is used (log line or observable table rows in docker-compose Postgres)
- [ ] `grep -r "Opc.UaFx" Helix.Orchestration.Agent/` returns no matches (commercial OPC UA package fully removed)
- [ ] SiteServer `/api/campaigns` and `/api/experiments` endpoints return JSON (not 500) when backed by a running Postgres instance
- [ ] All eight issues listed in CONCERNS.md "Recommended Immediate Actions" are resolved

### Additional verification

```
# A1: Worker.cs gone
test -f Helix.Orchestration.Recorder/Worker.cs && echo "FAIL" || echo "A1 OK"

# A2: single IObjectStorageSink
find . -name "IObjectStorageSink.cs" -not -path "*/bin/*" -not -path "*/obj/*" | wc -l
# expect: 1

# A5: no commercial OPC UA
grep -r "Opc.UaFx" . --include="*.csproj" --include="*.cs"
# expect: no matches

# A6: no deprecated API
grep -r "FormatterServices" . --include="*.cs"
# expect: no matches

# A8: no build exclusions
grep -c "Build Solution" Helix.Orchestration.slnx
# expect: 0

# A4: Cleipnir tables present (requires docker compose up -d first)
docker exec helix-postgres psql -U helix -d helix_orchestration -c "\dt" 2>/dev/null | grep -i "rf" | wc -l
# expect: > 0
```

---

## Commit Strategy

Each task can be committed atomically in wave order. Suggested commit messages:

```
fix: replace FormatterServices.GetUninitializedObject with RuntimeHelpers (A6)
fix: remove Opc.UaFx.Advanced commercial package and dead OpcTransport (A5)
fix: enable all projects in Debug configuration in slnx (A8)
chore: delete dead Worker.cs placeholder from Recorder (A1)
chore: verify IObjectStorageSink interface unification (A2)
fix: add CapabilityRegistry startup diagnostic log (A3)
feat: replace InMemoryFunctionStore with Cleipnir PostgreSQL store (A4)
feat: register AddHelixInfrastructure in SiteServer DI (A7)
chore: verify docker-compose.yml consolidation (A9)
```

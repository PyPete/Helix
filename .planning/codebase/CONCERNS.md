# Codebase Concerns

**Analysis Date:** 2026-05-26

---

## In-Progress Refactors

### Device Layer Architecture Replacement (Phase 13 → current)

The git status shows a significant mid-flight refactor of the device integration layer:

**Deleted (D) — old composition-via-inheritance model:**
- `Helix.Orchestration/Helix.Orchestration.Devices/BaseClasses/DeviceBase.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/BaseClasses/ModbusDeviceBase.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/BaseClasses/OpcDeviceBase.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/BaseClasses/SerialDeviceBase.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/BaseClasses/SetPointDeviceBase.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/BaseClasses/StirrerDeviceBase.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/BaseClasses/TcpDeviceBase.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/BaseClasses/VacPumpDeviceBase.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/Drivers/KnauerAzura.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/Drivers/SystagOpcUaClient.cs`
- `Helix.Orchestration/Helix.Orchestration.Devices/Drivers/VapourtecR4.cs`
- `Helix.Orchestration/tests/Helix.Orchestration.Devices.Tests/Drivers/VapourtecR4Tests.cs`

**New untracked (?) — new composition-via-DeviceAdapterBase model:**
- `Helix.Orchestration.Devices/` (root-level stub project — empty csproj, no source)
- `Helix.Orchestration.Infrastructure/` (root-level duplicate — partial content)
- `Helix.Orchestration.Recorder/` (root-level duplicate with full implementation)
- `Helix.Orchestration/Helix.Orchestration.Infrastructure/Storage/` (new storage layer)
- `Helix.Orchestration/Helix.Orchestration.Recorder/` (new recorder project — stub only)
- `Helix.Orchestration/tests/Helix.Orchestration.Recorder.Tests/` (new test project — empty)
- `docs/` and `tests/` at root level

The three original device drivers (`VapourtecR4`, `KnauerAzura`, `SystagOpcUaClient`) have been deleted from the `Helix.Orchestration/` tree but no replacement drivers implementing the new `DeviceAdapterBase` composition pattern exist yet. The solution currently has **zero concrete device driver implementations** in the active solution tree.

### Duplicate Project Trees

There are two parallel implementation trees present simultaneously:

| Concern | Root-level path | `Helix.Orchestration/` path | State |
|---|---|---|---|
| Recorder | `Helix.Orchestration.Recorder/` (full: Program.cs, services, models, config) | `Helix.Orchestration/Helix.Orchestration.Recorder/` (stub: Worker.cs only) | Root has full impl, solution has stub |
| Infrastructure | `Helix.Orchestration.Infrastructure/` (partial: Storage + Persistence) | `Helix.Orchestration/Helix.Orchestration.Infrastructure/` (full) | Diverged |
| Devices | `Helix.Orchestration.Devices/` (empty csproj, no source) | `Helix.Orchestration/Helix.Orchestration.Devices/` (full transport layer) | Root is placeholder only |

The `Helix.Orchestration.slnx` references only the `Helix.Orchestration/` subtree. Root-level duplicates are not in the solution. The Recorder test project at `tests/Helix.Orchestration.Recorder.Tests/` references the root-level `Helix.Orchestration.Recorder/` (full implementation), not the solution's stub.

---

## Technical Debt

| Area | Issue | Severity |
|---|---|---|
| Recorder | `Helix.Orchestration/Helix.Orchestration.Recorder/Worker.cs` is a placeholder loop (`Task.Delay(30s)` only) — the full MQTT+writer pipeline from the root-level project has not been merged into the solution | High |
| Devices | Zero concrete `DeviceAdapterBase` implementations exist for `VapourtecR4`, `KnauerAzura`, or `SystagOpcUaClient` after the old base class tree was deleted; device capability dispatch is entirely non-functional in the solution | High |
| DI lifetime | `DeviceAdapterExtensions.cs` registers transports as `Transient` but devices are captured by a `Singleton` `CapabilityRegistry` — transports become effectively singleton without being declared as such; incompatible with hot-reconnect scenarios | High |
| `CapabilityRegistry` empty | `AddMigratedDeviceAdapters()` builds a `CapabilityRegistry` singleton but never registers any devices into it (`Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` lines 17–22) — all capability invocations will find nothing | High |
| Durable workflow storage | `InMemoryFunctionStore` (Cleipnir) is used for durable runspace execution in `Helix.Orchestration.Agent/Program.cs`; all durable workflow state is lost on process restart | High |
| NATS.Net unused | `NATS.Net` is a declared dependency in `Helix.Orchestration.Core.csproj` but has zero usages in source; trend ingestion uses MQTT only | Medium |
| `IObjectStorageSink` interface divergence | Two incompatible `IObjectStorageSink` interfaces exist: root-level version (`WriteAsync(key, stream, contentType, ct)`) and `Helix.Orchestration/` version (`UploadObjectAsync(bucket, object, stream, ct)`). The root-level recorder uses the root-level interface; the solution's `InfrastructureServiceExtensions` registers the `Helix.Orchestration/` implementation | Medium |
| `FormatterServices.GetUninitializedObject` deprecated | Used in `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs` line 112 — this API is obsolete in .NET 5+ and removed in .NET 8+ | Medium |
| OPC UA dual library | Both `Opc.UaFx.Advanced` (commercial, Agent.csproj) and `OPCFoundation.NetStandard.Opc.Ua.Client` (open-source, Devices.csproj) are present. The Agent's `OpcTransport.cs` still uses `Opc.UaFx.Advanced`; the Devices layer uses the OPC Foundation library. Two OPC UA clients in the same process | Medium |
| `AutoAcceptUntrustedCertificates = true` | `OpcDeviceTransport.cs` line 80 sets `AutoAcceptUntrustedCertificates = true` in the OPC UA application configuration — this disables certificate validation for all OPC UA connections | Medium |
| Scalar / image / video / derived trends | `InMemoryTrendProvider` throws `NotSupportedException` for `GetTrend<T>`, `GetScalarTrend<T>`, `GetImageTrend`, `GetVideoTrend`, `GetDerivedTrend`, and `RegisterDerivedTrend` — 6 of 10 interface methods are unimplemented stubs | Medium |
| Campaign persistence not wired | `CampaignManager` in SiteServer uses only in-memory `ConcurrentDictionary`; `IExperimentRepository`/`ICampaignRepository` from Infrastructure are never injected into SiteServer (`Program.cs`); PostgreSQL persistence built in Phase 12 is not used by the running server | Medium |
| `TrendAggregator` scalar routing gap | `TrendAggregator.ProcessAsync` silently discards all scalar `TrendValue` types with a `LogDebug` — scalar device measurements from hardware never reach the trend provider | Low |
| `DeviceMeasurement` not routed to `ITrendProvider` | Device drivers' `GetMeasurements()` results are not fed into any `ITrendProvider` — device telemetry is unreachable from workflows | Low |
| Solution build flags | In `Helix.Orchestration.slnx`, most projects have `<Build Solution="Debug|*" Project="false" />` — Agent, Contracts, Infrastructure, Recorder, Simulation projects are excluded from Debug solution builds, making local F5 development unreliable | Low |

---

## Missing Implementations

- `Helix.Orchestration/Helix.Orchestration.Recorder/Worker.cs`: placeholder `BackgroundService` — the MQTT ingestion and batch-writer pipeline (present in `Helix.Orchestration.Recorder/`) is not yet integrated into the solution project
- `Helix.Orchestration/Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`: `CapabilityRegistry` is constructed but no devices are ever registered into it — entire device capability layer is inert
- `VapourtecR4` (no file in solution): `GetMeasurements()` live read-back (GF/GT commands), `SelectReagent`/`SelectSolvent` valve encoding, `GetStatus()` polling, fault detection, pump configuration
- `KnauerAzura` (no file in solution): correct command vocabulary, response validation (ACK/NAK), gradient/UV configuration, run state polling
- `SystagOpcUaClient` (no file in solution): setpoint write methods, correct OPC node path conventions, OPC UA change subscriptions
- `InMemoryTrendProvider`: scalar trends, image trends, video trends, derived trends (6 methods throw `NotSupportedException`)
- `SimulatedTrendProvider.RegisterDerivedTrend`: throws `NotImplementedException`
- `SimulatedTrendProvider.GetDiodeArraySlice` (line 305): throws `NotImplementedException` for diode-array chromatogram extraction
- `ISerialTransport.Open(portName, baudRate)`: the `baudRate` parameter exists on `SerialTransport` but the `ISerialTransport` interface `Open(portName)` signature does not accept baud rate — the `VapourtecR4.BaudRate` override value (19200) cannot be passed through the interface

---

## Inconsistencies

- **`IObjectStorageSink` interface shape**: root-level `Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs` defines `WriteAsync(key, stream, contentType, ct)` (single-key, keyless bucket); the `Helix.Orchestration/Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs` defines `UploadObjectAsync(bucket, object, stream, ct)` plus download/exists/delete methods. The two implementations are incompatible.
- **`MinioObjectStorageSink` implementation**: root-level uses the official `Minio` NuGet SDK (`Minio.DataModel.Args`); `Helix.Orchestration/` version uses `AWSSDK.S3` (`Amazon.S3`). Different clients, different auth config shapes (`MinioOptions` record vs class).
- **OPC UA library**: `Helix.Orchestration.Agent/Transport/OpcTransport.cs` uses `Opc.UaFx.Advanced` (commercial). `Helix.Orchestration.Devices/Transports/Opc/OpcFoundationSession.cs` uses `OPCFoundation.NetStandard.Opc.Ua.Client` (open-source). Docs (PHASE13) state the swap to OPC Foundation was completed, but the Agent still references the commercial library.
- **`CapabilityInvocationResult` type**: `Helix.Orchestration.Agent/Adapters/Devices/DeviceCapabilityAdapter.cs` calls `CapabilityInvocationResult.Succeeded(...)` and `CapabilityInvocationResult.Failed(...)` — these are from `Helix.Orchestration.Core.Capabilities`. `Helix.Orchestration.Devices/Abstractions/IHelixAdapter.cs` defines its own `CapabilityInvocationResult` with `Successful(...)` factory method — two types with the same name and different shapes exist.
- **Recorder project location**: the fully implemented recorder (`MqttRecorderService`, `RecorderWriterService`, `RecordedEvent`, `RecorderOptions`) lives outside the solution at the repo root; the solution-internal recorder is a placeholder stub.
- **`docker-compose.yml`**: root-level file adds MinIO (Phase 14); `Helix.Orchestration/docker-compose.yml` has Mosquitto + Postgres but no MinIO. Two diverged compose files.

---

## Test Gaps

- **Recorder integration tests (solution tree)**: `Helix.Orchestration/tests/Helix.Orchestration.Recorder.Tests/` directory exists but contains no test files (bin/obj only). The recorder test project in the solution has no test coverage.
- **Device driver tests deleted**: `tests/Helix.Orchestration.Devices.Tests/Drivers/VapourtecR4Tests.cs` was deleted with the driver files. No driver-level tests exist for any device driver.
- **`DeviceCapabilityAdapter` not tested**: no tests cover the capability dispatch logic or the empty `CapabilityRegistry` registration bug.
- **`TrendAggregator` scalar routing not tested**: the silent scalar discard path has no test coverage.
- **Infrastructure repositories (Docker-gated)**: all `PostgresRepositoriesTests` tests have `[Fact(Skip = "Requires Docker.")]` — they never run in CI.
- **CI excludes several test projects**: `Helix.Orchestration.Devices.Tests`, `Helix.Orchestration.Infrastructure.Tests`, and `Helix.Orchestration.Recorder.Tests` have `<Build Solution="Debug|*" Project="false" />` in the slnx; they also appear to be excluded from CI builds (`dotnet test` runs the full slnx which includes projects with build=false).
- **`SimulatedTrendProvider` edge cases**: no tests for `RegisterDerivedTrend` (throws) or the diode-array chromatogram extraction path.

---

## Outdated / Deprecated

- **`FormatterServices.GetUninitializedObject`**: used in `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs` line 112. This API was marked `[Obsolete]` in .NET 7 and annotated for removal. The correct replacement for constructing options objects is `Activator.CreateInstance<TOptions>()` or using `options.Value` pattern with `IOptions<T>`.
- **`Opc.UaFx.Advanced` commercial package**: still referenced in `Helix.Orchestration.Agent.csproj` after the documented decision (PHASE13) to replace it with `OPCFoundation.NetStandard.Opc.Ua.Client`. The commercial package remains as a dependency.
- **`System.IO.Ports` Version `9.0.5`**: projects target `net10.0` but `System.IO.Ports` is pinned to `9.0.5`. Should track `10.*` for framework consistency.
- **`Microsoft.Extensions.Configuration.Abstractions` Version `9.*`** (Infrastructure.csproj): inconsistent with the `net10.0` target framework; should align to `10.*`.
- **`Microsoft.Extensions.DependencyInjection.Abstractions` Version `9.*`** (Infrastructure.csproj): same version drift.
- **`docker-compose.yml` format `version: '3.8'`**: the `version` field is deprecated in Docker Compose v2 and ignored; root compose uses it while the `Helix.Orchestration/` version uses `version: "3.9"`.

---

## Configuration / Security Concerns

- **Default MinIO credentials in committed `appsettings.json`**: `Helix.Orchestration.Recorder/appsettings.json` commits `AccessKey: "minioadmin"` and `SecretKey: "minioadmin"` with `UseSsl: false`. These are the default MinIO root credentials. While tagged as dev defaults, they are present in source and mirror the docker-compose environment variables exactly.
- **OPC UA `AutoAcceptUntrustedCertificates = true`**: `Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs` line 80 disables certificate validation for all OPC UA connections. This bypasses the OPC UA security model entirely and must be replaced before production use.
- **MQTT no authentication**: `MqttRecorderService` connects to the broker with `WithCleanSession()` only — no username/password, no TLS, no client certificate. Mosquitto is configured for anonymous access.
- **PostgreSQL dev password in source**: `helix_dev_password` appears in `Helix.Orchestration/docker-compose.yml` and in `PostgresRepositoriesTests.cs` (hardcoded in test fixture). Acceptable for dev-only but must not be re-used in staging/production.
- **MinIO `UseSsl: false`** default: object storage transport is unencrypted by default; Phase 14 handover acknowledges "MinIO/S3 auth hardening and TLS defaults for production" as a known gap.

---

## Recommended Immediate Actions

1. **Merge the root-level Recorder implementation into the solution** — `Helix.Orchestration.Recorder/` (full, with `MqttRecorderService`, `RecorderWriterService`, models, config) must replace the stub `Helix.Orchestration/Helix.Orchestration.Recorder/Worker.cs`. The solution currently ships a non-functional recorder.

2. **Resolve `IObjectStorageSink` interface divergence** — decide which interface shape is canonical (`WriteAsync` vs `UploadObjectAsync`) and remove the divergent copy. The Recorder and Infrastructure projects must agree on a single interface and client library (Minio SDK vs AWSSDK.S3).

3. **Re-implement at least one device driver using `DeviceAdapterBase`** — `VapourtecR4` is the highest-priority target; without it the device integration layer cannot be validated end-to-end. The `DeviceAdapterExtensions` `CapabilityRegistry` must also be populated.

4. **Replace `InMemoryFunctionStore` with a durable backend (PostgreSQL)** — the Cleipnir `InMemoryFunctionStore` in `Helix.Orchestration.Agent/Program.cs` means all durable runspace state is lost on agent restart. The PostgreSQL store from the Cleipnir library should be used before any production deployment.

5. **Remove or suppress the commercial `Opc.UaFx.Advanced` dependency** — per the Phase 13 migration decision, the OPC Foundation library is the chosen replacement. The Agent `OpcTransport.cs` should be migrated to use the `OpcDeviceTransport` / `OpcFoundationSession` from the Devices project.

6. **Replace `FormatterServices.GetUninitializedObject`** in `DevicesServiceCollectionExtensions.cs` — use `Activator.CreateInstance<TOptions>()` to avoid calling a deprecated/removed API on .NET 10.

7. **Wire Infrastructure repositories into SiteServer** — `IExperimentRepository` and `ICampaignRepository` are implemented and tested but `Helix.Orchestration.SiteServer/Program.cs` never registers `AddHelixInfrastructure`. Campaign and experiment data is ephemeral (in-memory only).

8. **Fix solution build configuration** — most projects have `<Build Solution="Debug|*" Project="false" />` in `Helix.Orchestration.slnx`. This silently skips building Agent, Recorder, Infrastructure, Devices, Simulation, and Contracts in Debug configuration, masking compile errors during local development.

---

*Concerns audit: 2026-05-26*

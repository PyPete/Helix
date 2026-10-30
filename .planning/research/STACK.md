# Technology Stack

**Project:** Helix Vision Mark II  
**Researched:** 2026-10-29  
**Confidence:** HIGH for the existing stack; MEDIUM for new standards/library recommendations (external documentation lookup was unavailable in this run).

## Recommendation in One Sentence

Extend the existing **.NET 10 local Agent + PostgreSQL + gRPC/protobuf + MQTT + React/Tauri** system; make PostgreSQL the durable scientific-system record and object storage the immutable source-artifact store, while keeping hardware behind capability ports and adopting standards incrementally at import/export boundaries rather than rebuilding around a LIMS standard.

## Recommended Stack

### Core Framework

| Technology | Version | Purpose | Why |
|---|---:|---|---|
| C# / .NET | `net10.0` (existing) | Agent, domain, Site Server, recorder, device adapters | Do not introduce a second backend runtime. The repo already has mature ASP.NET Core hosts, DI, gRPC, durable-workflow, device, testing, and deployment seams. The Agent remains the only process allowed to invoke hardware. |
| ASP.NET Core minimal APIs + hosted services | .NET 10 | Agent-local HTTP/gRPC command boundary; workers for reconciliation, ingestion, and campaign progression | Fits the existing composition model. Run long-lived work in cancellable hosted services/durable functions, not request handlers or desktop processes. |
| Cleipnir.ResilientFunctions + PostgreSQL backend | `4.*` (existing) | Durable waits, campaign state transitions, restart-aware execution | Retain it for bounded campaign orchestration, but persist explicit domain records for campaign policy, decision, sample, result, approval, and reconciliation state. A workflow engine is not the scientific audit model. |
| React + TypeScript + Vite | React 18 desktop / React 19 dashboard (existing) | Operator experience | Retain. UI submits proposals/approvals and renders authoritative Agent/domain state; it must never encode safety authority or durable orchestration. |
| Tauri | `2` (existing) | Installed local desktop shell | Retain for the single-workstation UI. It is a shell and local configuration surface, not a device-driver host; Agent HTTP/gRPC remains the authority boundary. |
| Protobuf + gRPC | existing `Grpc.* 2.*`, `Google.Protobuf 3.*` | Versioned Agent-control contracts | Keep as the typed control-plane protocol. Add fields compatibly, reserve removed tags, carry stable IDs and explicit result/status messages; do not serialize unbounded CSV rows or raw artifacts through control RPCs. |

### Database

| Technology | Version | Purpose | Why |
|---|---:|---|---|
| PostgreSQL + Npgsql/Dapper | existing Npgsql `9.*`, Dapper `2.*` | Authoritative operational/scientific records | Use the existing database rather than SQLite for the core record: it already supports durable functions and campaign persistence. Make it the transaction boundary for idempotency keys, imported-result revisions, decisions, approvals, outbox records, and state-machine transitions. |
| PostgreSQL relational schema with JSONB payload extensions | PostgreSQL deployment version must be pinned and supported | Apparatus topology, typed ports, constraints, provenance, and versioned policy payloads | Use normalized tables for identities, lineage, revisions, relationships, statuses, timestamps, and uniqueness. Use JSONB only for schema-versioned device-specific settings/raw parsed payloads. Put query-critical values in typed columns and enforce foreign keys/check constraints. |
| S3-compatible object store (MinIO locally) | existing AWSSDK.S3 `4.0.23.4` | Immutable original CSV/artifacts and large recorder data | Store original bytes, content type, SHA-256, source name, import timestamp, parser version, and object key. Database records reference the artifact and resulting immutable revision. This provides reproducibility without putting analytical files or trend blobs in relational rows. |

### Infrastructure

| Technology | Version | Purpose | Why |
|---|---:|---|---|
| MQTT broker + MQTTnet | MQTTnet `4.*` (existing) | Trend/telemetry distribution | Retain for volatile observation streams, with versioned topic namespaces, QoS selected per stream, retained messages only for latest state, and a bounded durable-ingestion path where data loss is unacceptable. MQTT is not the campaign command log or scientific system of record. |
| PostgreSQL transactional outbox worker | New application component, no broker change | Publish domain events after committed writes | Write an outbox row atomically with campaign/sample/result/decision transitions, then publish asynchronously with idempotent consumers. This closes the current fire-and-forget MQTT gap and avoids claiming a physical action or result revision before durable recording. |
| OpenTelemetry + Serilog | existing `OpenTelemetry 1.*`, Serilog 8/file/console sinks | Correlated observability and forensic audit support | Continue existing instrumentation but configure an OTLP exporter/collector for deployments. Put `campaignId`, `iterationId`, `runspaceId`, `apparatusRevisionId`, `sampleId`, `resultRevisionId`, `deviceId`, and correlation ID on logs/traces. Emit append-only safety/approval/audit events to durable storage, not only console output. |
| Docker Compose for services; native Windows desktop installation | existing | Reproducible simulation/developer stack and local workstation deployment | Keep containers for Postgres, MQTT, MinIO, Agent simulation, Site Server, Recorder. Treat physical-agent deployment as a Windows service/managed local process with machine-scoped configuration, certificate material, and least-privilege account; do not containerize a device driver merely for uniformity. |

### Supporting Libraries and Standards

| Library / Standard | Version | Purpose | When to Use |
|---|---:|---|---|
| `System.Text.Json` source generation + explicit schema versions | .NET 10 | Domain payload serialization | Default for persisted device configuration, import manifests, and API DTOs. Avoid polymorphic/typeless deserialization for data that might originate in CSV or UI input. |
| FluentValidation **or** endpoint-local validation with domain validators | Current stable version, phase-pin | Boundary validation | Choose one convention only. Validate syntax at API/import boundaries, then apply deterministic domain validation (capability, units, policy, lease/resource/safety) immediately before command dispatch. Existing compiler validation is the model. |
| CsvHelper | Current stable version, phase-pin | Controlled CSV parsing | Add for analytical import only. Configure explicit delimiter/culture/header mapping, byte/row/field limits, strict required columns, bad-data callbacks, and streaming parsing. Preserve raw bytes first; never let a parser overwrite a reviewed result. |
| UCUM codes plus SI quantities | Standard, no NuGet dependency required initially | Canonical units for parameters/results | Persist a numeric value plus UCUM unit code and retain source unit/value. Centralize conversion/compatibility in a domain `Quantity` service; reject dimensionally incompatible mappings. Do not invent free-text unit strings. |
| OPC UA .NET Standard client | existing `OPCFoundation.NetStandard.Opc.Ua.Client 1.5.374.86` | Industrial instrument connectivity | Prefer OPC UA when equipment exposes it. Require trusted certificates, endpoint allowlists, namespace/node mappings under version control, reconnect/state semantics, and a simulator/contract test before a device capability is enabled. |
| FluentModbus / `System.IO.Ports` / TCP transports | existing | Legacy device protocols | Keep strictly behind `Devices` adapters. Each driver declares capabilities, preconditions, timeouts, retry/abort behavior, safety classification, and a safe state. Serial/Modbus have no inherent identity or robust semantics; never expose them above the Agent. |
| AnIML and Allotrope Data Format (ADF) | Interoperability targets, not MVP dependencies | Future analytical-data interchange | Do **not** make either the first importer. CSV + review is the correct MVP. Preserve enough metadata/provenance to export/map later; assess a specific vendor format only when an integration requires it. |
| ISA-88-inspired equipment/procedure separation | Conceptual modeling pattern | Apparatus vs procedure/campaign model | Model apparatus topology and operating constraints independently from procedures and experiment plans. Do not import the full ISA-88 standard as a framework; apply its separation of equipment and procedural control to Helix's existing capability boundary. |

## Data and Messaging Decisions

### Durable scientific record

Use immutable/revisioned domain tables for `ApparatusRevision`, `CampaignPolicyRevision`, `ExperimentPlanRevision`, `Campaign`, `CampaignIteration`, `ExecutionAttempt`, `Sample`, `SampleRelationship`, `AnalyticalRequest`, `AnalyticalImport`, `AnalyticalResultRevision`, `CalculationRevision`, `Decision`, and `Approval`. Rows may supersede earlier rows; never update approved scientific evidence in place. The current row/status may be a projection, while the revision/audit record is authoritative.

Every externally retried action must carry an idempotency key scoped to its operation (import hash + request, sample confirmation, decision emission, Agent submission). Each physical command must have a dispatch ID and reconciliation outcome; after a restart, observe hardware/device state and record the outcome rather than replaying a potentially completed command.

### CSV analytical import

Use a staged pipeline: **artifact upload → virus/size/type checks → immutable object storage → parse to staging → template/header/unit/sample matching → validation/quarantine → scientist review → immutable result revision → approved-event outbox**. Include parser/template versions and exact source row references in provenance. Locale, decimal separator, timestamps, units, duplicated headers, and sample identifiers are import metadata—not assumptions. CSV injection defenses are required in any later spreadsheet export.

### MQTT role

Use MQTT for telemetry/trends and notifications, not commands which authorize physical work. Define `helix/v1/{site}/{hood}/{device}/...` topic contracts, payload schema version, timestamp/source timestamp, sequence number where available, and QoS/retention decision per topic. Persist a backpressure/loss policy. The existing recorder drops messages on a full bounded channel, so its output cannot be represented as complete scientific evidence until durability/acknowledgement semantics are fixed.

## Device Integration Recommendation

1. **Capability-first:** continue `ICapabilityInvoker`/registry. A plan names logical capabilities and required capability versions; no campaign or UI references serial addresses, node IDs, or vendor SDK types.
2. **Device manifest:** add a versioned, administrator-owned manifest mapping device instance → driver → transport config → available capabilities → parameter schemas → limits/interlocks → calibration/status requirements. Bind apparatus ports to device-instance capability roles, not direct protocol configuration.
3. **Safety envelope:** Agent validates policy budget, termination condition, capability preconditions, device health, lease/fencing token, resource reservation, parameter limits, and safety interlocks immediately before dispatch. The same checks run for autonomous and manual executions.
4. **Driver lifecycle:** adapters implement connect/health/read state/execute/abort-or-safe-stop/reconcile. Device completion must be observed and recorded; a transport-level acknowledgement alone is insufficient.
5. **Rollout:** simulator first, then deterministic fake/contract tests, then a controlled hardware qualification. Remove or gate incomplete OceanView/Pico registrations: the codebase map shows they can compile while throwing at runtime or lack a native DLL.

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|---|---|---|---|
| Core runtime | Extend .NET 10/C# | Python orchestration/notebook service | Python is valuable for offline scientific analysis later, but introducing it now would split execution authority, type/contracts, deployment, and safety testing. Invoke a bounded external analysis worker only after a reproducible calculation contract exists. |
| Desktop device authority | Agent as local service | Tauri/Rust commands or browser-to-device | It bypasses the existing durable/safety/capability boundary and dies with the UI. |
| System of record | PostgreSQL + object storage | SQLite-only workstation DB | SQLite may be appropriate for a disposable local cache, but it would duplicate durability, migration, and recovery logic while Helix already requires PostgreSQL for durable execution and uses service boundaries. |
| Queue/event backbone | PostgreSQL outbox + existing MQTT | Adopt NATS/Kafka now | NATS is declared but unused; Kafka/NATS adds operations and duplicate semantics without solving the MVP. Establish a durable outbox and explicit MQTT delivery policy first. Revisit only for multi-hood/federated throughput. |
| Analytical interchange | Reviewed CSV now; optional AnIML/ADF adapters later | Full LIMS/CDS or arbitrary PDF ingestion | Both exceed the declared scope and complicate validation/provenance. CSV establishes the correct review and immutable-evidence model first. |
| Apparatus modeling | Typed Helix model with ISA-88-inspired separation | Full ISA-88 or ontology platform | Full standards implementation would slow the single-fumehood MVP. Keep a stable internal model, version it, and map/export when external integration warrants it. |

## Installation / Configuration Direction

```bash
# Existing backend projects are NuGet-managed; keep package versions pinned by project.
dotnet restore Helix.Orchestration.slnx

# Proposed CSV dependency (add only in the analytical-import phase, after evaluating license/current version)
dotnet add Helix.Orchestration.Infrastructure package CsvHelper

# Existing front ends
cd helix-desktop && npm ci
cd ../helix-dashboard && npm ci
```

Pin exact production versions during the implementing phase (the repository currently uses several floating `*` package ranges). Generate a dependency lock file or centrally managed package versions, publish SBOMs, and qualify native/vendor SDKs separately. Do not commit broker, database, S3, or device credentials to `appsettings*.json`.

## Implementation Risks That Affect Stack Choices

- **Control-plane security is presently incomplete:** codebase mapping found no Agent/Site Server authentication/authorization. Until mTLS/local loopback or a trusted private network and authenticated identities are implemented, keep Agent control APIs network-private. CORS is not authorization.
- **Current in-memory leases/runspaces/state are not restart/multi-instance safe:** campaigns requiring reliable recovery need durable ownership/fencing and reconciliation records, not only a durable function store.
- **Recorder loss behavior is incompatible with scientific evidence:** it currently discards MQTT events under load. Treat trends as observational only until durable ingestion semantics and fault tests exist.
- **Vendor/native drivers are supply-chain and safety boundaries:** package/verify DLLs, permit only administrator-managed absolute paths, and test safe-stop/recovery against hardware.
- **Mixed target/package versions:** projects target .NET 10 but some `Microsoft.Extensions.*` references are 9.x. Align the framework package family in a dedicated dependency-hardening phase to prevent binding/runtime surprises.

## Sources

### Repository evidence — HIGH confidence

- `.planning/PROJECT.md` (Mark II constraints, local-first authority and MVP scope; mapped 2026-10-29).
- `.planning/codebase/STACK.md` (existing runtimes/dependencies; mapped 2026-10-29).
- `.planning/codebase/ARCHITECTURE.md` (capability, Agent, gRPC, MQTT, persistence boundaries; mapped 2026-10-29).
- `.planning/codebase/INTEGRATIONS.md` and `CONCERNS.md` (current authentication, recorder, driver, and durability risks; mapped 2026-10-29).
- Project files under `Helix.Orchestration.*/*.csproj` (target frameworks and declared package versions; inspected 2026-10-29).

### External primary references — MEDIUM confidence; verify current version/implementation during phase planning

- Microsoft, “Hosted services in ASP.NET Core”: https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services
- Microsoft, “Worker services in .NET”: https://learn.microsoft.com/dotnet/core/extensions/workers
- PostgreSQL documentation (constraints, JSON types, transaction isolation): https://www.postgresql.org/docs/current/
- OASIS, MQTT Version 5.0 specification: https://docs.oasis-open.org/mqtt/mqtt/v5.0/mqtt-v5.0.html
- OPC Foundation, OPC UA specifications: https://reference.opcfoundation.org/
- NIST, International System of Units (SI): https://www.nist.gov/pml/owm/metric-si/si-units
- UCUM specification: https://ucum.org/ucum
- Allotrope Foundation, Data Format overview: https://www.allotrope.org/
- ASTM AnIML standard landing page: https://www.astm.org/e2077-00r24.html

External documentation-provider and web-search calls were unavailable (Context7 absent; Brave returned HTTP 403), so these URLs are supplied as phase-planning verification targets rather than as a claim of current API-level verification.

# Phase 01: Agent Safety Boundary - Pattern Map

**Mapped:** 2026-10-29
**Scope:** Candidate existing edit points and implied new files from 01-CONTEXT.md and 01-RESEARCH.md. Neither input defines a final new-file manifest; proposed names below are planner-owned suggestions, NOT existing symbols.
**Primary analog families:** 5 (compiler, runspace, repository, native commands, timeline), with supporting endpoint/test excerpts.
**Tracked-source gate:** Every existing source path used as an analog below appears in the repository's `git ls-files` output obtained during this mapping. No install/runtime mirrors, generated schemas, bin/obj, or coverage copies are analogs.

## File Classification

Legend: **E** = existing tracked path, candidate modification/integration; **N** = proposed new path, not an existing contract; **C** = conditional edit, only if the planner retains that execution/transport/deployment path. “Exact” describes role/data flow, NOT proof that existing semantics are safe. Family references identify excerpts in Pattern Assignments. Rows grouping paths assign the stated classification to each listed file.

### Existing integration points

| New/Modified File | Status | Role | Data Flow | Closest Analog / assignment | Match Quality |
|---|---|---|---|---|---|
| `Helix.Orchestration.Agent/Program.cs` | E | config | event-driven; request-response | Existing composition; A2 + endpoint boundary | exact structure |
| `Helix.Orchestration.Agent/appsettings.json` | E | config | request-response | Existing Kestrel section; shared listener pattern | exact structure |
| `Helix.Orchestration.Agent/Api/DeviceEndpoints.cs` | E | controller | request-response | Existing `MapDeviceEndpoints`; endpoint boundary | exact structure |
| `Helix.Orchestration.Agent/Api/WorkflowEndpoints.cs` | E | controller | transform; request-response | Existing compile handler + A1 | exact |
| `Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs` | E | controller | request-response | Existing lifecycle handlers + A2 | exact structure |
| `Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs` | E | controller | request-response | Existing submission handler + A2 | exact structure |
| `Helix.Orchestration.Agent/gRPC/Server/AgentGrpcService.cs` | E | controller | request-response | Existing service + endpoint boundary | exact structure |
| `Helix.Orchestration.Contracts/Protos/agent_control.proto` | C | model | request-response | Existing protobuf messages/service | exact structure |
| `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` | E | config | event-driven | Existing registration + simulator notes | exact structure |
| `Helix.Orchestration.Agent/Adapters/Devices/HelixAdapterCapabilityBridge.cs` | E | service | transform; request-response | Existing adapter bridge | exact structure |
| `Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs` | E | service | request-response | Existing logical dispatch seam; A2 integration | exact structure |
| `Helix.Orchestration.Core/Safety/SafetyEngine.cs` | E | service | event-driven; batch | Existing evaluation + A1 diagnostic structure | role-match |
| `Helix.Orchestration.Core/WSL/WslCompiler.cs` | E | service | transform | A1, existing compiler | exact |
| `Helix.Orchestration.Core/WorkflowEngine/CapabilityReadExtensions.cs` | E | utility | request-response; transform | Existing read helper, fail-open behavior must change | exact structure |
| `Helix.Orchestration.Core/WorkflowEngine/VariableSubstitution.cs` | E | utility | transform | Existing substitution, observation restriction required | exact structure |
| `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs` | C | service | event-driven | A2, existing state-machine structure | exact structure |
| `Helix.Orchestration.Core/Execution/WorkflowExecutor.cs` | C | service | event-driven | A2 + existing executor cancellation structure | role-match |
| `Helix.Orchestration.Agent/Execution/WslExecutionAdapter.cs` | E | service | transform; event-driven | Existing adapter, integrate or retire + A2 | role-match |
| `Helix.Orchestration.Agent/Experiments/ExperimentManager.cs` | E | service | event-driven | A2; immutable record replacement/logging only | role-match |
| `Helix.Orchestration.Devices/Drivers/SimulatorDevice.cs` | E | service | request-response; streaming | Existing simulator metadata/observation | exact structure |
| `Helix.Orchestration.Simulation/SimulationServiceExtensions.cs` | C | config | event-driven | Existing singleton/hosted-service registration | exact structure |
| `Helix.Orchestration.Agent/Experiments/ExperimentEventPersistenceWorker.cs` | E | service | event-driven | A3; scoped repository access only | role-match |
| `Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs` | E | config | CRUD | Existing scoped registrations + A3 | exact |
| `helix-desktop/src-tauri/src/lib.rs`; `helix-desktop/src-tauri/src/commands/mod.rs` | E | config | request-response | A4 command-module wiring | exact structure |
| `helix-desktop/src-tauri/src/commands/connection.rs` | E | service | request-response | A4 IPC shape only | exact structure |
| `helix-desktop/src-tauri/build.rs`; `helix-desktop/src-tauri/Cargo.toml` | C | config | transform | A4 native build declarations | role-match |
| `helix-desktop/src-tauri/capabilities/default.json`; `helix-desktop/src-tauri/tauri.conf.json` | E | config | request-response | Existing main-window scope/config; A4 | role-match |
| `helix-desktop/src/types/agent.ts` | E | model | transform | A5 discriminated events; explicit new evidence DTO | role-match |
| `helix-desktop/src/hooks/useAgentHealth.ts`; `helix-desktop/src/hooks/useAgentWebSocket.ts`; `helix-desktop/src/hooks/useExperiment.ts` | E | hook | request-response; streaming | Health state excerpt; replace transport with A4 client | role-match |
| `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx`; `helix-desktop/src/components/DeviceManualControl/DeviceManualControl.tsx`; `helix-desktop/src/shell/HelixShell.tsx` | E | component | request-response; event-driven | Existing UI state/error boundary + A4/A5 | role-match |
| `helix-desktop/src/components/DeviceRegistry.tsx`; `helix-desktop/src/shell/panels/DeviceExplorer.tsx`; `helix-desktop/src/components/WorkflowBuilder/DevicePalette.tsx`; `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` | E | component | request-response | Existing read callers; move all through A4 client | role-match |
| `helix-desktop/src/components/ConnectionConfig.tsx` | E | component | request-response; file-I/O | Existing error UI; do not copy arbitrary host selection | role-match |
| `helix-desktop/src/shell/panels/ExperimentTimeline.tsx`; `helix-desktop/src/shell/panels/panels.css` | E | component | event-driven; transform | A5 render structure | exact structure |
| `docker-compose.yml`; `deploy/systemd/helix-agent.service`; `deploy/windows/install-agent-service.ps1` | C | config | request-response | Existing launcher/listener settings, shared listener notes | exact structure |

### Implied new responsibilities (suggested names)

| New File | Status | Role | Data Flow | Closest Analog / assignment | Match Quality |
|---|---|---|---|---|---|
| `Helix.Orchestration.Core/Safety/CommandAdmissionEvaluator.cs` | N | service | transform | A1 `WslCompiler.cs` | exact role/flow |
| `Helix.Orchestration.Core/Safety/CommandAdmissionResult.cs`; `Helix.Orchestration.Core/Safety/CommandAdmissionContext.cs` | N | model | transform | A1 `CompilationDiagnostic.cs` / `CompilationResult.cs` | role-match |
| `Helix.Orchestration.Core/Execution/CommandIntent.cs`; `Helix.Orchestration.Core/Execution/CommandEvidence.cs`; `Helix.Orchestration.Core/Execution/ExecutionControlState.cs` | N | model | event-driven | A1 records + existing event record shape in A3 | role-match |
| `Helix.Orchestration.Core/Execution/ICommandDispatcher.cs`; `Helix.Orchestration.Core/Execution/ICommandLedger.cs`; `Helix.Orchestration.Core/Execution/ISimulatorObservation.cs` | N | service (ports) | request-response; CRUD | Existing cancellable Core lease port / A3 repository port shape | role-match |
| `Helix.Orchestration.Agent/Security/DesktopSessionAuthenticationHandler.cs`; `Helix.Orchestration.Agent/Security/DesktopSessionService.cs` | N | middleware; service | request-response | None for verified identity/authentication | none |
| `Helix.Orchestration.Agent/Execution/AgentCommandDispatcher.cs`; `Helix.Orchestration.Agent/Execution/ExecutionControlService.cs` | N | service | request-response; event-driven | A2 `DurableRunspace.cs`, structure only | role-match |
| `Helix.Orchestration.Agent/Execution/CommandReconciliationWorker.cs` | N | service | batch; event-driven | Existing persistence worker's hosted-service/scope shape | role-match; no recovery algorithm analog |
| `Helix.Orchestration.Agent/Api/CommandEvidenceEndpoints.cs` | N | controller | request-response | Existing `WorkflowEndpoints.cs` typed results + A3 | exact role/flow |
| `Helix.Orchestration.Infrastructure/Persistence/PostgresCommandLedger.cs`; `Helix.Orchestration.Infrastructure/Persistence/PostgresSimulatorStateStore.cs` | N | service | CRUD | A3 `PostgresExperimentEventRepository.cs` | exact role/flow; transaction semantics new |
| `postgres/migrations/003_agent_command_ledger.sql` | N | migration | CRUD; transform | `002_experiment_events.sql`, A3 | exact role/flow |
| `helix-desktop/src-tauri/src/commands/agent.rs` | N | service | request-response; streaming | A4 `commands/connection.rs` IPC structure only | role-match |
| `helix-desktop/src/lib/agentClient.ts` | N | service | request-response; streaming | A4 native command bridge; no authenticated renderer client exists | partial |
| `helix-desktop/src/hooks/useCommandEvidence.ts` | N | hook | request-response; event-driven | `useAgentHealth.ts` state/cleanup shape + A5 | role-match |
| `helix-desktop/src/components/AgentSession/AgentSession.tsx` | N | component | request-response | `ConnectionConfig.tsx` alert/validation UI | exact role/flow; bootstrap new |

### Test scope

| New/Modified File | Status | Role | Data Flow | Closest Analog / assignment | Match Quality |
|---|---|---|---|---|---|
| `tests/Helix.Orchestration.Core.Tests/Safety/SafetyEngineTests.cs` | E | test | event-driven | Existing safety tests, test excerpts | exact |
| `tests/Helix.Orchestration.Core.Tests/Safety/CommandAdmissionTests.cs` | N | test | transform | Existing safety tests + A1 | role-match |
| `tests/Helix.Orchestration.Agent.Tests/Api/AgentDeviceEndpointsTests.cs` | E | test | request-response | Existing TestServer host | exact structure |
| `tests/Helix.Orchestration.Agent.Tests/Integration/AgentAuthenticationTests.cs`; `tests/Helix.Orchestration.Agent.Tests/Integration/DispatchBoundaryTests.cs`; `tests/Helix.Orchestration.Agent.Tests/Integration/DispatchRaceTests.cs`; `tests/Helix.Orchestration.Agent.Tests/Integration/CommandEvidenceTests.cs` | N | test | request-response; event-driven | Agent TestServer + SiteServer substitution factory | role-match |
| `tests/Helix.Orchestration.Agent.Tests/Execution/ExecutionControlTests.cs`; `tests/Helix.Orchestration.Agent.Tests/Execution/ReconciliationTests.cs` | N | test | event-driven; batch | Same host/test structure; restart proof new | role-match |
| `tests/Helix.Orchestration.Agent.Tests/Integration/E2eWorkflowExecutionTests.cs` | E | test | event-driven | Same host/test structure; replace placeholder scope | role-match |
| `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs`; `tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj` | E | test; config | CRUD; file-I/O | Existing PostgreSQL fixture/migration packaging | exact structure |
| `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/CommandLedgerTests.cs` | N | test | CRUD; event-driven | Same PostgreSQL fixture, fail setup visibly | exact role/flow |
| `helix-desktop/src/shell/panels/ExperimentTimeline.test.tsx`; `helix-desktop/src/hooks/useAgentHealth.test.ts`; `helix-desktop/src/hooks/useAgentWebSocket.test.ts`; `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.test.tsx`; `helix-desktop/src/components/DeviceManualControl/DeviceManualControl.test.tsx`; `helix-desktop/src/shell/HelixShell.test.tsx`; `helix-desktop/src/components/ConnectionConfig.test.tsx` | E | test | request-response; event-driven | A5 RTL/Vitest suite | role-match |
| `helix-desktop/src/lib/agentClient.test.ts`; `helix-desktop/src/hooks/useCommandEvidence.test.ts`; `helix-desktop/src/components/AgentSession/AgentSession.test.tsx` | N | test | request-response; event-driven | A5 RTL/Vitest suite, mock native client not fetch | role-match |
| `helix-desktop/src-tauri/src/commands/agent_tests.rs` | N | test | request-response | No inspected native security/transport test analog | none |

**Reference-only, not assumed edits:** Core `IDeviceLeasingService`, `RunspaceStatus`, `CompilationDiagnostic`, `CompilationResult`, existing event repository/migration, and SiteServer `HelixWebApplicationFactory` are analog/reference sources. Existing in-memory lease/global-state stores require replacement of authoritative usage, not necessarily modification of their implementations. Do not create a SiteServer campaign dependency in this phase. If changing `RunspaceStatus` rather than adding separate durable controls, inventory every enum consumer explicitly.

## Pattern Assignments

### A1 — Admission evaluator, diagnostic/context models, compiler integration

**Primary analog:** `Helix.Orchestration.Core/WSL/WslCompiler.cs`.
**Apply to:** All A1 rows, Core intent/control/evidence records, and compile-time validation.

Imports / namespace (lines 1-4):
```csharp
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Safety;

namespace Helix.Orchestration.Core.WSL;
```

Aggregate validation and fail-before-transformation (lines 31-50, excerpt):
```csharp
var diagnostics = new List<CompilationDiagnostic>();

// 1. Schema/version validation
ValidateSchema(document, diagnostics);

// 2. Node graph structural validation
ValidateGraph(document, diagnostics);

// 3. Node parameter validation
ValidateNodeParameters(document, diagnostics);

// 4. Device binding resolution
ValidateDeviceBindings(document, deviceRegistry, diagnostics);

// 5. Variable reference validation
ValidateVariableReferences(document, diagnostics);

// If any errors, return failed result before running transforms
if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
    return CompilationResult.Failed(diagnostics);
```

Diagnostic record (`Helix.Orchestration.Core/WSL/CompilationDiagnostic.cs`, lines 10-14):
```csharp
public sealed record CompilationDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    string? NodeId = null,
    string? Code = null);
```

Result factories (`Helix.Orchestration.Core/WSL/CompilationResult.cs`, lines 28-33, documentation omitted):
```csharp
public static CompilationResult Succeeded(RunspaceDefinition definition, IEnumerable<CompilationDiagnostic> diagnostics)
    => new(true, definition, diagnostics.ToList());

public static CompilationResult Failed(IEnumerable<CompilationDiagnostic> diagnostics)
    => new(false, null, diagnostics.ToList());
```

**Copy:** Focused validators, immutable diagnostic records, stable check codes and aggregate expected failures. Introduce typed admission dispositions/check evidence; a boolean invocation success is not sufficient to represent reject/hold/uncertain effect. Keep evaluator deterministic and independent of ASP.NET/driver/database composition; Agent supplies fresh context. Evaluate all capability, safety, lease, resource, budget, termination and health requirements at proposal and dispatch boundaries.

**Do not copy:** `InjectSafetyEnvelope` lines 237-261 synchronously reads historical violations and emits warnings/guard nodes; it is not runtime authorization. Node parameters at lines 157-166 only validate IDs/types. Capability bounds/finite numbers/units and supported active-executor constructs need new enforcement.

### A2 — Agent dispatch, durable controls, execution-path integration

**Primary analog:** `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs`.
**Apply to:** A2 rows and new Agent dispatch/control services; recovery uses only its concurrency shape, not replay behavior.

Imports (lines 1-6):
```csharp
using Helix.Orchestration.Agent.Telemetry;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Agent.Runspaces;
```

Dependencies/concurrency (lines 13-15):
```csharp
private readonly IWorkflowExecutor _workflowExecutor;
private readonly SemaphoreSlim _gate = new(1, 1);
private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
```

Constructor guards (lines 41-45):
```csharp
public DurableRunspace(RunspaceDefinition definition, IWorkflowExecutor workflowExecutor)
{
    Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    _workflowExecutor = workflowExecutor ?? throw new ArgumentNullException(nameof(workflowExecutor));
}
```

Async gate/validation shape (lines 50-54 and 64-67, separate excerpts):
```csharp
await _gate.WaitAsync(cancellationToken);
try
{
    if (Status is not RunspaceStatus.Idle and not RunspaceStatus.Paused)
        throw new InvalidOperationException($"Cannot start runspace in state {Status}.");
```
```csharp
finally
{
    _gate.Release();
}
```

**Copy:** Cancellable async APIs, constructor guards, explicitly gated local transitions, asynchronous continuations, telemetry tags (`ExecuteAsync`, lines 122-124). Add database serialization/fences for durable control/reservations; `SemaphoreSlim` alone is NOT restart-safe ownership.

**Error semantics:** Existing `WorkflowExecutor.cs` lines 60-68 distinguishes cancellation and unexpected exceptions before rethrowing. Preserve that distinction; dispatch exceptions/cancellation after intent can mean unknown effect, not rejection. Do not put retry around simulator mutation.

**Critical wiring assignments:**
- `Program.cs` lines 89-112 constructs the actual `IRunspaceManager` using `InMemoryRunspace`; line 186 directly calls `capRegistry.InvokeAsync`. Route this actual factory through the authoritative dispatcher and durable cursor. Merely modifying separately registered `WorkflowExecutor` (line 247) cannot close the default bypass.
- `DeviceEndpoints.cs` lines 150-163 invokes the registry before emitting manual evidence. Replace with actor + canonical intent + admission + persist-before-effect dispatch; no direct registry mutation dependency in the endpoint.
- `RunspaceEndpoints.cs` lines 24-37 accepts raw definitions and starts them. Revalidate/canonicalize through the same gate or explicitly reject unsupported raw submissions.
- `AgentGrpcService.cs` lines 21-33 accepts serialized definitions via `_runspaceManager.CreateAsync`. Its acknowledgement pattern is transport formatting, not authorization. Preserve `ServerCallContext.CancellationToken` and make errors typed/redacted.
- `WslExecutionAdapter.cs` lines 39-68 uses `document.Compile()`, acquires leases, directly constructs a durable runspace and releases leases only in catch. Integrate or retire this alternative, not another admission implementation.
- `ExperimentEndpoints.cs` lines 38-44 creates an empty stub graph; desktop `ExecutionPanel.tsx` lines 93-100 checks compilation but submits only a name. Both sides must deliver a real nonempty validated workflow.
- `ExperimentManager.cs` lines 61-99 replaces in-memory lifecycle records; use its `with`/structured-log style only. Hold/abort authority and cursor must live in durable control records.
- `CapabilityReadExtensions.cs` lines 34-73 calls any supplied invoker and catches/defaults failure. `VariableSubstitution.cs` lines 52-63 can reach device resolution and replaces unknown values with empty strings. Restrict to explicit read-only observation and return unknown/failure rather than safe-looking defaults.
- Existing `RunspaceStatus.cs` lines 6-24 values are **Idle, Running, Paused, Completed, Faulted, Disposed**. `DurableRunspace.ResumeAsync` lines 90-91 delegates to start; abort lines 104-105 becomes Faulted. Neither is an analog for durable held/terminal-abort/restart-cursor semantics.

### A3 — Ledger, simulator persistence, evidence endpoint, recovery worker, migration

**Primary analog:** `Helix.Orchestration.Infrastructure/Persistence/PostgresExperimentEventRepository.cs`.
**Apply to:** New PostgreSQL services/ledger ports/migration and evidence/recovery integration.

Imports and append boundary (lines 1-8):
```csharp
using Dapper;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class PostgresExperimentEventRepository(DbConnectionFactory connectionFactory) : IExperimentEventRepository
{
    public async Task AppendAsync(ExperimentEventRecord record, CancellationToken ct = default)
    {
```

Parameterized SQL / connection ownership (lines 15-24):
```csharp
            VALUES (
                @ExperimentId, @Phase, @EventType, @RunspaceId,
                @StepId, @StepLabel, @IsManual, @DeviceId, @Capability,
                @ParametersJson::jsonb, @ResultJson::jsonb, @OccurredAt
            );
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: ct));
```

Canonical ordering / mapping (`GetByExperimentIdAsync`, lines 34-42):
```csharp
            FROM experiment_events
            WHERE experiment_id = @experimentId
            ORDER BY sequence_num ASC;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<ExperimentEventRow>(new CommandDefinition(sql, new { experimentId }, cancellationToken: ct));
        return rows.Select(r => r.ToRecord()).ToList();
```

Repository port shape (`IExperimentEventRepository.cs`, lines 3-10):
```csharp
public interface IExperimentEventRepository
{
    Task AppendAsync(ExperimentEventRecord record, CancellationToken ct = default);

    Task<IReadOnlyList<ExperimentEventRecord>> GetByExperimentIdAsync(string experimentId, CancellationToken ct = default);

    Task<IReadOnlyList<ExperimentEventRecord>> GetReplayLogAsync(string experimentId, CancellationToken ct = default);
}
```

**Copy:** Raw SQL strings, parameters, `CommandDefinition` cancellation, `await using`, row-to-record mapping, durable sequence ordering. Put new domain-facing ports in Core even though the legacy port above resides in Infrastructure. Reuse `DbConnectionFactory.cs` lines 10-22 snake_case mapping/connection creation and `InfrastructureServiceExtensions.cs` lines 11-17 singleton factory + scoped repositories.

**New correctness work, not supplied by this analog:** One connection/transaction with consistent lock order for control revision, durable leases, resource/budget reservations and immutable intent; unique command/content/evidence identities; separate outcome commit; durable cursor; append-only review annotations; read cursor paging. Never hold the transaction across a long simulator effect, never fabricate legacy actors, never downgrade storage failure to unaudited dispatch. A global BIGSERIAL identity alone does not establish per-execution commit order under concurrent transactions; serialize the canonical stream deliberately.

Migration shape (`postgres/migrations/002_experiment_events.sql`, lines 18-23 and 34-35):
```sql
CREATE TABLE IF NOT EXISTS experiment_events (
    sequence_num    BIGSERIAL       PRIMARY KEY,
    experiment_id   TEXT            NOT NULL,
    phase           TEXT            NOT NULL DEFAULT 'none',
    event_type      TEXT            NOT NULL,
    runspace_id     TEXT            NOT NULL DEFAULT '',
```
```sql
CREATE INDEX IF NOT EXISTS ix_experiment_events_experiment_id
    ON experiment_events(experiment_id, sequence_num);
```

Copy migration naming/style and JSONB/TIMESTAMPTZ types, not the legacy schema as a complete command ledger. New constraints/access rules must enforce immutable facts and idempotency. Existing-volume migrations need explicit application: compose init scripts alone only initialize new databases. Infrastructure test project lines 29-31 currently copies only `001_initial.sql`; package every required migration for the new fixture.

Worker scope pattern (`ExperimentEventPersistenceWorker.cs`, lines 19-27):
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (var evt in channel.Events.ReadAllAsync(stoppingToken))
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExperimentEventRepository>();
            await repo.AppendAsync(MapToRecord(evt), stoppingToken);
```

Copy async hosted-service cancellation and scoped dependency resolution into reconciliation worker. Do NOT copy the worker's lines 29-32 warning-and-drop catch for authoritative command facts. Recovery must scan durable incomplete intents, establish held state, observe without mutation, append matching/nonmatching/unreadable evidence and advance only the confirmed command cursor. Matching clears reconciliation hold only, never independent hold/abort. Publish notifications after commit, not before.

### A4 — Native broker/client/session integration

**Primary analog:** `helix-desktop/src-tauri/src/commands/connection.rs`.
**Apply to:** Native broker/module wiring, one renderer client, session UI, and every existing Agent read/write/stream caller.

Imports, serde DTO, IPC result shape (lines 1-17):
```rust
use serde::{Deserialize, Serialize};
use tauri::AppHandle;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AgentConnectionConfig {
    pub host: String,
    pub port: u16,
    pub protocol: String,
}

#[tauri::command]
pub async fn load_config(_app: AppHandle) -> Result<AgentConnectionConfig, String> {
    Ok(AgentConnectionConfig {
        host: "localhost".to_string(),
        port: 5080,
        protocol: "http".to_string(),
    })
}
```

Native registration (`helix-desktop/src-tauri/src/lib.rs`, lines 4-14):
```rust
mod commands;
use commands::connection::{load_config, save_config};

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_store::Builder::new().build())
        .plugin(tauri_plugin_fs::init())
        .invoke_handler(tauri::generate_handler![load_config, save_config])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
```

**Copy:** Module/command registration, serde DTOs, async `Result` boundary. **Not present:** Credential storage, scientist verification, revocation/expiry, fixed-target authenticated native gRPC client, scoped native security tests. `save_config` is currently a no-op. The native build script only calls `tauri_build::build()`; manifest `tonic`/`tonic-build` declarations are not generated-client proof. Resolve bootstrap and generation/build gates before freezing protocol/dependencies; preserve the explicit manifest instruction against casually adding prost.

**Apply safely:** Fixed loopback destination, native-only credentials, typed operation allowlist and trusted-main-window argument/scope checks. Never forward an arbitrary renderer URL or return a secret to JavaScript. `capabilities/default.json` names `main`, but existing permissions do not prove custom application commands are scoped; add deliberate manifest/permission enforcement. `tauri.conf.json` currently has null CSP; reconcile renderer security with the selected broker design.

**Renderer assignment:** `agentClient.ts` is proposed, NOT an existing library. Provide typed health/discovery/compile/submit/control/evidence/stream operations; migrate every caller listed in classification, including reads. No raw `fetch`/WebSocket fallback. Co-locate tests and mock native invocation/client. UI stores display/session status only. `ConnectionConfig.tsx` currently allows arbitrary host/port and persists renderer configuration: do not copy that into credential-bearing broker scope.

### A5 — Outcome-first timeline, evidence hook, renderer tests

**Primary analog:** `helix-desktop/src/shell/panels/ExperimentTimeline.tsx`.
**Apply to:** Timeline/CSS/type rows, new evidence hook and related component/tests.

Imports / props (lines 1-9):
```tsx
import type { ReactNode } from 'react';
import type { RunspaceEvent, ExperimentEvent } from '../../types/agent';
import './panels.css';

export interface ExperimentTimelineProps {
  runspaceEvents: RunspaceEvent[];
  experimentEvents: ExperimentEvent[];
  activeExperimentId: string | null;
}
```

Discriminated union (lines 33-35):
```tsx
type RunspaceItem = { kind: 'runspace'; event: RunspaceEvent };
type ExperimentItem = { kind: 'experiment'; event: ExperimentEvent };
type TimelineItem = RunspaceItem | ExperimentItem;
```

Accessible panel/list shell (lines 130-136):
```tsx
return (
  <section aria-label="Experiment Timeline" data-testid="experiment-timeline">
    <ul data-testid="timeline-list">
      {listItems}
    </ul>
  </section>
);
```

**Copy:** Relative type imports, named local export, semantic section/list, discriminated DTO handling and stable test hooks. **Replace:** Active-only empty gating (16-21), timestamp-only sorting (42-47), index-derived keys, volatile arrays and phase suppression. Query completed/durable history by cursor; merge by evidence identity and canonical sequence. New summary includes action, capability/device, time, verified actor and outcome; expandable details retain parameters/checks/IDs/observation. Critical outcomes need text/icons plus color. Annotations append attributable events, never mutate facts.

Remote error state (`helix-desktop/src/hooks/useAgentHealth.ts`, lines 27-38):
```tsx
} catch (e) {
  setState({
    healthy: false,
    error: (e as Error).message,
    lastChecked: new Date(),
  });
}
};
check();
const id = setInterval(check, intervalMs);
return () => clearInterval(id);
}, [baseUrl, intervalMs]);
```

Copy stateful error/cleanup shape, not renderer HTTP. Render user-visible errors using `ConnectionConfig.tsx` line 33's `role="alert"` pattern. Evidence hooks additionally need stale-request cancellation/identity protection and reconnect cursor handling.

## Shared Patterns

### Authentication / endpoint boundary

**No existing authenticated analog found:** C# search for `AuthenticationHandler`, `AddAuthentication`, `UseAuthentication`, and `FallbackPolicy` returned no matches. Use the framework design in RESEARCH.md, not fabricated code attributed to this repository. All API reads/writes/gRPC/health and WebSocket upgrade require a validated session; denied attempts are audited without recording credentials. A claimed actor header is not a verified identity.

Existing boundary (`Helix.Orchestration.Agent/Program.cs`, lines 316-325):
```csharp
var app = builder.Build();

app.UseCors();
app.UseWebSockets();
app.MapGrpcService<AgentGrpcService>();
app.MapHealthChecks("/health");
app.MapWorkflowEndpoints();
app.MapRunspaceEndpoints();
app.MapDeviceEndpoints();
app.MapExperimentEndpoints();
```

This is an **edit location, not a secure pattern**. Authentication/authorization ordering must precede stream acceptance. `/ws` is handled by custom middleware at lines 329-347 and accepts at line 335: endpoint fallback policy alone does not automatically secure that custom middleware. Explicitly protect/test it or migrate it to an authorized endpoint. Test every exposed transport, not just device POST.

Typed endpoint response pattern (`WorkflowEndpoints.cs`, lines 33-38):
```csharp
var result = compiler.Compile(document, deviceRegistry, safetyEngine);

if (result.Success)
    return Results.Ok(result.Definition);

return Results.UnprocessableEntity(result.Diagnostics);
```

Copy typed expected-error responses, `MapGroup`/`WithTags`/`Produces` conventions, and cancellation injection (lines 24-29). Decide structured reject/hold/control payloads explicitly. Do not return arbitrary internal exception messages or collapse unknown effect into 500/rejected success=false.

### Simulator / logical capability boundary

`HelixAdapterCapabilityBridge.cs` lines 20-32 strips the namespace, normalizes JSON and awaits the Devices adapter. Keep this translation downstream of admission, inaccessible as a second mutation port to endpoints/workflows. Existing registry lines 53-59 catches every exception into failed result: cancellation/uncertainty requires deliberate treatment.

Simulator metadata (`Helix.Orchestration.Devices/Drivers/SimulatorDevice.cs`, lines 94-99):
```csharp
new CoreParameterSchema("flowRate", "Flow Rate", CoreParameterType.Numeric, "mL/min",
    Min: 0, Max: 10, Step: 0.1, Options: null, DefaultValue: 1.0)
],
IsDestructive: false,
RequiresActiveRunspace: false,
EstimatedDurationMs: 500),
```

Use existing declared parameter names/units/bounds as inputs, not automatic approved-policy defaults. Setter lines 30-37 only logs/succeeds; GetFlow lines 40-44 reads the unchanged field. Implement a deterministic parameter-aware effect plus durable independent observation/effect identity before recovery tests. Existing constant `HealthCheckAsync` success is not fresh state evidence.

`DeviceAdapterExtensions.cs` lines 17-55 registers real serial/network/OPC adapters. Agent `Program.cs` line 68 calls that registration: simulator-only execution requires removing their reachability, not hiding them in UI. `SimulationServiceExtensions.cs` lines 19-34 demonstrates concrete singleton + interface forwarding/hosted services, but it represents a separate simulation stack; do not assume it is the active `SimulatorDevice` registration or install another unguarded `ICapabilityInvoker`.

### Listener / deployment configuration

Agent `appsettings.json` lines 9-14 uses `http://localhost:5080`; systemd lines 22-28 and Windows installer lines 40-43 repeat local listener settings. Copy configuration ownership, not credentials. Compose lines 77-81 publishes `5080:5080` and overrides Kestrel to `0.0.0.0`; lines 90-92 probes anonymous `/health`. Remove/reconcile wildcard exposure and anonymous probes. Host-loopback vs container-loopback are different namespaces: retaining a containerized Agent needs an explicit topology decision, not just a textual localhost replacement. Verify effective IPv4/IPv6/protocol listeners and all overrides. Installed services/data remain unobserved; no deployment changes were performed during mapping.

### Logging / durable evidence distinction

`ExperimentManager.cs` line 42 supplies structured fields:
```csharp
logger.LogInformation("Experiment {ExperimentId} '{Name}' started.", experimentId, request.Name);
```
Use injected typed logging and named fields for operations. Logs/telemetry are NOT canonical evidence. `SafetyEngine.cs` lines 83-88 cancels E-stop and writes Console output; preserve stop behavior but add structured/auditable treatment, avoiding database waits while holding its synchronous state lock. Unknown safety state must hold rather than infer safe from an empty historical violation list.

### Tests: host composition, explicit effects, durable infrastructure

Agent TestServer source: `tests/Helix.Orchestration.Agent.Tests/Api/AgentDeviceEndpointsTests.cs`, lines 41-45 and 55-63:
```csharp
var builder = WebApplication.CreateBuilder();

builder.WebHost.UseTestServer();

builder.Services.AddRouting();
```
```csharp
var app = builder.Build();

app.MapDeviceEndpoints();

await app.StartAsync();

var client = app.GetTestClient();

return (app, client);
```

**Apply to:** Authentication/boundary/evidence/control test rows. Copy async disposable host/client testing. Extract/reuse production middleware/routes/dispatch composition so the test does not secure a different host. Use a spy simulator, deterministic clock and explicit intent/effect/outcome barriers. Denied operations must assert zero effects AND recorded failed checks/security attempt. Add nonempty workflow submissions, variable-read bypass tests and native scope/loopback tests. Keep actual reconciliation worker in restart tests.

Substitution source: `tests/Helix.Orchestration.SiteServer.Tests/HelixWebApplicationFactory.cs`, lines 26-33:
```csharp
// Replace PostgreSQL repositories with in-memory stubs — no connection string needed
services.RemoveAll<IExperimentRepository>();
services.RemoveAll<ICampaignRepository>();
services.RemoveAll<IExperimentEventRepository>();
services.RemoveAll<DbConnectionFactory>();
services.AddScoped<IExperimentRepository, InMemoryExperimentRepository>();
services.AddScoped<ICampaignRepository, InMemoryCampaignRepository>();
services.AddScoped<IExperimentEventRepository, InMemoryExperimentEventRepository>();
```

Copy explicit dependency replacement, not this factory's blanket `RemoveAll<IHostedService>()` for restart acceptance and not its no-op audit repository. Agent startup initializes PostgreSQL before `Build` (`Program.cs` 76-77); ordinary service substitution alone cannot intercept that work. Provide a deliberate composition seam, with real storage retained in durability acceptance.

Core safety test source: `tests/Helix.Orchestration.Core.Tests/Safety/SafetyEngineTests.cs`, lines 77-91:
```csharp
var failingRule = new InterlockRule(() => Task.FromResult(false))
{
    RuleId = "FailingRule",
    Description = "Critical failure",
    Severity = InterlockSeverity.Critical
};

engine.RegisterInterlock(passingRule);
engine.RegisterInterlock(failingRule);

// Act
var result = await engine.EvaluateInterlocksAsync();

// Assert
Assert.False(result);
```

Copy real domain collaborators and async xUnit assertions. Add a parameterized six-check truth table plus health/unknown cases, compiled-vs-fresh-state races, expired leases, competing reservations/budget and abort fence. Prefer deterministic barriers over the existing monitoring test's timed sleeps.

PostgreSQL fixture source: `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs`, lines 65-76:
```csharp
ConnectionFactory = new DbConnectionFactory(connStr);

var migrationPath = Path.Combine(AppContext.BaseDirectory, "migrations", "001_initial.sql");
if (!File.Exists(migrationPath))
    throw new FileNotFoundException($"Migration file not found at {migrationPath}");

var sql = await File.ReadAllTextAsync(migrationPath);

await using var conn = ConnectionFactory.Create();
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand(sql, conn);
await cmd.ExecuteNonQueryAsync();
```

Copy Testcontainers/CI PG connection, migration loading and async cleanup. Replace caught setup failure/`IsAvailable` early returns (lines 78-81, 116-117). Required ledger/restart tests must fail setup visibly or report an explicit acceptance-blocking skip. Recreate Agent/simulator against the same real database and test before-intent, after-intent, after-effect/before-outcome, and after-outcome/before-ack windows; verify no duplicate dispatch.

Renderer suite source: `helix-desktop/src/shell/panels/ExperimentTimeline.test.tsx`, lines 1-16:
```tsx
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { ExperimentTimeline } from './ExperimentTimeline';
import type { RunspaceEvent, ExperimentStartedEvent, ExperimentEvent } from '../../types/agent';

describe('ExperimentTimeline', () => {
  it('renders empty state when no activeExperimentId', () => {
    render(
      <ExperimentTimeline
        runspaceEvents={[]}
        experimentEvents={[]}
        activeExperimentId={null}
      />
    );
    expect(screen.getByTestId('timeline-empty')).toBeInTheDocument();
  });
```

Copy RTL/Vitest imports, typed fixtures and accessible assertions; update active-only expectation. Add critical outcomes, expandable details, annotations, completed-history selection, reconnect/deduplication, canonical order and visible session/error cases. Native security tests and packaged native smoke have no inspected working analog; they are new acceptance work.

## No Analog Found

| File / responsibility | Role | Data Flow | Reason / planner action |
|---|---|---|---|
| Proposed desktop-session authentication handler/service | middleware; service | request-response | No framework-authentication implementation found; follow RESEARCH.md framework references and confirm pairing/scientist bootstrap. |
| Proposed native broker credential/transport internals and native tests | service; test | request-response; streaming | Existing commands only implement config stubs; generator, session scope/secret storage and native build must be proven. |
| New ledger transactional fencing/reservations | service | CRUD; event-driven | Append repository is a shape analog, not an atomic admission/fence implementation. Define migrations/transactions and race tests. |
| Reconciliation algorithm and restart-surviving simulator postconditions | service | batch; request-response | Existing resume/field state cannot establish recovery; follow locked D-07–D-09 and research algorithm, with real database crash tests. |

## Metadata

**Analog search scope:** Tracked Core, Agent API/runtime/adapters, Infrastructure/Persistence, Simulation, Devices simulator, Contracts protobuf, desktop renderer/native, managed/frontend tests, migrations and launch configuration. Source content searches additionally checked C# authentication and all desktop `fetch`/WebSocket call sites.
**Extraction:** Five primary analogs read once; supporting source inspected in non-overlapping numbered ranges. Required canonical planning references and project AGENTS.md read. Project skill-directory glob found no SKILL.md indexes.
**Coverage interpretation:** Role/flow matches are structural only. Authentication, fenced durability and reconciliation are explicit gaps, never asserted as existing guarantees. Candidate paths are not a mandatory broad rewrite; planner may retire bypass paths and narrow the slice, but all retained exposed routes/callers must close the boundary.
**Validation status:** No builds/tests/live listener/database/native probes run by this mapper. Research baseline passes do not validate new safety behavior.
**Writes:** Only this PATTERNS.md; no source, STATE.md, commit or branch changes.

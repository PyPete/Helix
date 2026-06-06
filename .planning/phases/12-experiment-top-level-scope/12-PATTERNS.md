# Phase 12: Experiment as Top-Level Scope — Pattern Map

**Mapped:** 2026-06-06
**Files analyzed:** ~25 new/modified files implied by I1–I7
**Analogs found:** strong analogs for 6 of 7 requirement areas; I2 (phase scopes) and I5 (replay) are gaps

---

## Summary of Key Findings

The codebase has strong foundations for Phase 12 but treats `ExperimentId` as a loose annotation on `RunspaceId` rather than as a first-class container. The core work is:

1. Promote `ExperimentId` to a proper identity with its own manager, lifecycle, and repository row.
2. Add `ExperimentPhase` (pre/main/post) as a scope discriminator carried on events and stored in a new `experiment_events` table.
3. Surface experiment lifecycle (not runspace lifecycle) on the Ribbon controls.
4. Add an `ExperimentTimeline` dock panel that reads the extended `RunspaceEvent` stream.

---

## File Classification

| New / Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `Helix.Orchestration.Core/Experiment/ExperimentPhase.cs` | model | — | `ExperimentLifecycle.cs` | exact |
| `Helix.Orchestration.Core/Experiment/IExperimentManager.cs` | service-interface | CRUD | `IRunspaceManager.cs` | role-match |
| `Helix.Orchestration.Core/Experiment/ExperimentManager.cs` | service | CRUD | `RunspaceManager.cs` | role-match |
| `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` *(modify)* | model | event-driven | self | exact |
| `Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs` | controller | request-response | `RunspaceEndpoints.cs` | exact |
| `Helix.Orchestration.Infrastructure/Persistence/IExperimentEventRepository.cs` | service-interface | CRUD | `IExperimentRepository.cs` | role-match |
| `Helix.Orchestration.Infrastructure/Persistence/PostgresExperimentEventRepository.cs` | service | CRUD | `PostgresExperimentRepository.cs` | role-match |
| `postgres/migrations/002_experiment_events.sql` | migration | — | `001_initial.sql` | exact |
| `helix-desktop/src/types/agent.ts` *(modify)* | model | — | self | exact |
| `helix-desktop/src/hooks/useAgentWebSocket.ts` *(modify)* | hook | event-driven | self | exact |
| `helix-desktop/src/hooks/useExperiment.ts` | hook | request-response | `useAgentHealth.ts` | role-match |
| `helix-desktop/src/shell/panels/ExperimentTimeline.tsx` | component | event-driven | `LogOutput.tsx` | role-match |
| `helix-desktop/src/shell/WorkspaceLayout.tsx` *(modify)* | component | — | self | exact |
| `helix-desktop/src/shell/HelixShell.tsx` *(modify)* | component | — | self | exact |
| `helix-desktop/src/shell/Ribbon.tsx` *(modify)* | component | — | self | exact |

---

## Pattern Assignments

### `Helix.Orchestration.Core/Experiment/ExperimentPhase.cs` (model)

**Analog:** `Helix.Orchestration.Core/Experiment/ExperimentLifecycle.cs`

Copy the enum pattern verbatim — one value per named phase scope:

**Full analog** (`ExperimentLifecycle.cs`, lines 1–30):
```csharp
namespace Helix.Orchestration.Core.Experiment;

public enum ExperimentLifecycle
{
    Created,
    Running,
    Suspended,
    Completed,
    Failed
}
```

New file should be:
```csharp
namespace Helix.Orchestration.Core.Experiment;

public enum ExperimentPhase
{
    Pre,
    Main,
    Post
}
```

---

### `Helix.Orchestration.Core/Experiment/IExperimentManager.cs` + `ExperimentManager.cs` (service, CRUD)

**Analog:** `Helix.Orchestration.Core/Runspace/IRunspaceManager.cs` + `RunspaceManager.cs`

**Interface pattern** (`IRunspaceManager.cs`, lines 1–30):
```csharp
using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Core.Runspace;

public interface IRunspaceManager : IAsyncDisposable
{
    Task<IRunspace> CreateAsync(RunspaceDefinition definition, CancellationToken cancellationToken = default);
    IRunspace? Get(string runspaceId);
    IReadOnlyList<IRunspace> ListActive();
    void Track(IRunspace runspace);
    Task AbortAsync(string runspaceId, CancellationToken cancellationToken = default);
}
```

**Implementation skeleton** (`RunspaceManager.cs`, lines 1–25):
```csharp
public sealed class RunspaceManager : IRunspaceManager
{
    private readonly ConcurrentDictionary<string, IRunspace> _runspaces = new();
    // ...
    public async Task<IRunspace> CreateAsync(RunspaceDefinition definition, CancellationToken cancellationToken = default)
    {
        var runspace = await _factory(definition, cancellationToken).ConfigureAwait(false);
        if (!_runspaces.TryAdd(runspace.RunspaceId, runspace))
        {
            await runspace.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Runspace id '{runspace.RunspaceId}' is already tracked.");
        }
        return runspace;
    }
    public IRunspace? Get(string runspaceId) { ... }
}
```

For `IExperimentManager`, the analogous shape is:
- `CreateAsync(name, metadata) → ExperimentContext` — generates a new `experimentId`, transitions to `Running`
- `GetById(experimentId) → ExperimentContext?`
- `ListActive() → IReadOnlyList<ExperimentContext>`
- `SetPhaseAsync(experimentId, ExperimentPhase)` — updates the active phase scope
- `CompleteAsync(experimentId)` / `FailAsync(experimentId, reason)`

**Key difference from RunspaceManager:** ExperimentManager does NOT wrap `IAsyncDisposable` on individual contexts — experiments persist to the repository even after completion.

---

### `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` *(modify)*

**Analog:** self — extend the existing discriminated union

**Existing base record** (`RunspaceEvent.cs`, lines 1–8):
```csharp
public abstract record RunspaceEvent(string Type, string RunspaceId, DateTimeOffset Timestamp);
```

**Existing ManualActionEvent** (`RunspaceEvent.cs`, lines 100–117):
```csharp
public sealed record ManualActionEvent(
    string DeviceId,
    string Capability,
    Dictionary<string, object> Parameters,
    bool Success,
    object? Result,
    string? Error)
    : RunspaceEvent("manual-action", "", DateTimeOffset.UtcNow);
```

**Changes needed:**

1. Add `ExperimentId` and `Phase` to the base record or as optional properties on a new intermediate:
   ```csharp
   // Option A: add to base (non-breaking, defaults to empty string / null)
   public abstract record RunspaceEvent(
       string Type,
       string RunspaceId,
       DateTimeOffset Timestamp,
       string? ExperimentId = null,          // NEW
       string? ExperimentPhase = null);      // NEW ("pre"/"main"/"post")
   ```

2. Add new experiment-lifecycle event types (parallel to RunspaceStartedEvent):
   ```csharp
   public sealed record ExperimentStartedEvent(string ExperimentId, string Phase)
       : RunspaceEvent("experiment-started", "", DateTimeOffset.UtcNow, ExperimentId, Phase);

   public sealed record ExperimentPhaseChangedEvent(string ExperimentId, string Phase)
       : RunspaceEvent("experiment-phase-changed", "", DateTimeOffset.UtcNow, ExperimentId, Phase);

   public sealed record ExperimentCompletedEvent(string ExperimentId)
       : RunspaceEvent("experiment-completed", "", DateTimeOffset.UtcNow, ExperimentId);

   public sealed record ExperimentFailedEvent(string ExperimentId, string Reason)
       : RunspaceEvent("experiment-failed", "", DateTimeOffset.UtcNow, ExperimentId);
   ```

3. Update `ManualActionEvent` to carry `ExperimentId?` and `ExperimentPhase?` so manual actions recorded inside an experiment scope are traceable (I3):
   ```csharp
   public sealed record ManualActionEvent(
       string DeviceId,
       string Capability,
       Dictionary<string, object> Parameters,
       bool Success,
       object? Result,
       string? Error,
       string? ExperimentId = null,     // NEW
       string? ExperimentPhase = null)  // NEW
       : RunspaceEvent("manual-action", "", DateTimeOffset.UtcNow, ExperimentId, ExperimentPhase);
   ```

**WebSocket broadcast is zero-change** — `BroadcastLoopAsync` in `AgentService.cs` (lines 107–143) serializes with `JsonSerializer.Serialize(evt, evt.GetType(), ...)` so the new fields propagate automatically.

---

### `Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs` (controller, request-response)

**Analog:** `Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs`

**Minimal API group registration pattern** (`RunspaceEndpoints.cs`, lines 16–22):
```csharp
public static class RunspaceEndpoints
{
    public static void MapRunspaceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/runspaces")
            .WithTags("Runspaces");
        // ...
    }
}
```

**POST handler pattern with DI + error wrapping** (`RunspaceEndpoints.cs`, lines 24–44):
```csharp
group.MapPost("/", async Task<IResult> (
    RunspaceDefinition definition,
    IRunspaceManager manager,
    IRunspaceEventEmitter? emitter,
    CancellationToken ct) =>
{
    try
    {
        var runspace = await manager.CreateAsync(definition, ct);
        emitter?.Publish(new RunspaceStartedEvent(runspace.RunspaceId));
        await runspace.StartAsync(ct);
        return Results.Ok(new { runspaceId = runspace.RunspaceId });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Failed to start runspace",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("CreateRunspace")
.WithSummary("Submit a RunspaceDefinition for execution")
.Produces<object>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status500InternalServerError);
```

**Sub-action POST pattern** (`RunspaceEndpoints.cs`, lines 63–90):
```csharp
group.MapPost("/{id}/pause", async Task<IResult> (
    string id,
    IRunspaceManager manager,
    CancellationToken ct) =>
{
    var rs = manager.Get(id);
    if (rs is null)
        return Results.NotFound(new { Error = $"Runspace '{id}' not found." });
    await rs.PauseAsync(ct);
    return Results.Ok();
})
.WithName("PauseRunspace")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);
```

**New `ExperimentEndpoints.cs` shape** (following this pattern):
```
POST /experiments           → CreateExperiment (I1)
GET  /experiments/{id}      → GetExperiment
POST /experiments/{id}/phase → SetPhase (I2)
POST /experiments/{id}/stop  → StopExperiment (I6)
GET  /experiments/{id}/replay → GetReplayLog (I5)
```

**Wire-up in `Program.cs`** mirrors how `RunspaceEndpoints` is registered (`Program.cs`, line approx. 165):
```csharp
app.MapWorkflowEndpoints();
app.MapRunspaceEndpoints();
app.MapDeviceEndpoints();
app.MapExperimentEndpoints();   // NEW
```

---

### `Helix.Orchestration.Infrastructure/Persistence/IExperimentEventRepository.cs` + `PostgresExperimentEventRepository.cs` (service, CRUD)

**Analog:** `IExperimentRepository.cs` + `PostgresExperimentRepository.cs`

**Interface pattern** (`IExperimentRepository.cs`, lines 1–13):
```csharp
public interface IExperimentRepository
{
    Task SaveAsync(ExperimentRecord record, CancellationToken ct = default);
    Task<ExperimentRecord?> GetByRunspaceIdAsync(string runspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<ExperimentRecord>> GetByCampaignIdAsync(string campaignId, CancellationToken ct = default);
    Task<IReadOnlyList<ExperimentRecord>> GetAllAsync(CancellationToken ct = default);
}
```

**Implementation upsert pattern** (`PostgresExperimentRepository.cs`, lines 10–35):
```csharp
public sealed class PostgresExperimentRepository(DbConnectionFactory connectionFactory) : IExperimentRepository
{
    public async Task SaveAsync(ExperimentRecord record, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO experiments (...)
            VALUES (...)
            ON CONFLICT (runspace_id) DO UPDATE SET ...;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: ct));
    }
}
```

**Private row class pattern** (`PostgresExperimentRepository.cs`, lines 75–95):
```csharp
private sealed class ExperimentRow
{
    public string RunspaceId { get; set; } = "";
    // ...
    public ExperimentRecord ToRecord() => new(...);
}
```

**New `IExperimentEventRepository` shape** (I4, I5):
```csharp
public interface IExperimentEventRepository
{
    Task AppendAsync(ExperimentEventRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<ExperimentEventRecord>> GetByExperimentIdAsync(
        string experimentId, CancellationToken ct = default);
    Task<IReadOnlyList<ExperimentEventRecord>> GetReplayLogAsync(
        string experimentId, CancellationToken ct = default);  // ordered by sequence_num
}

public sealed record ExperimentEventRecord(
    long SequenceNum,       // auto-increment for replay ordering
    string ExperimentId,
    string Phase,           // "pre"|"main"|"post"
    string EventType,       // mirrors RunspaceEvent.Type
    string RunspaceId,      // empty string for experiment-level events
    string? StepId,
    string? StepLabel,
    bool? IsManual,
    string? DeviceId,
    string? Capability,
    string? ParametersJson,
    string? ResultJson,
    DateTimeOffset Timestamp
);
```

**DI registration** (`InfrastructureServiceExtensions.cs`, lines 10–18):
```csharp
public static IServiceCollection AddHelixInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IExperimentRepository, PostgresExperimentRepository>();
    services.AddScoped<ICampaignRepository, PostgresCampaignRepository>();
    services.AddScoped<IExperimentEventRepository, PostgresExperimentEventRepository>();  // NEW
    return services;
}
```

---

### `postgres/migrations/002_experiment_events.sql` (migration)

**Analog:** `postgres/migrations/001_initial.sql`

**Pattern** (001_initial.sql, lines 1–12):
```sql
CREATE TABLE IF NOT EXISTS campaigns (
    campaign_id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    -- ...
);
CREATE INDEX IF NOT EXISTS ix_experiments_campaign_id ON experiments(campaign_id);
```

**New migration:**
```sql
-- Phase 12: experiment_events table for I4 (scope tree persistence) and I5 (replay log)
CREATE TABLE IF NOT EXISTS experiment_events (
    sequence_num    BIGSERIAL PRIMARY KEY,
    experiment_id   TEXT NOT NULL,
    phase           TEXT NOT NULL,          -- 'pre', 'main', 'post'
    event_type      TEXT NOT NULL,          -- mirrors RunspaceEvent.Type
    runspace_id     TEXT NOT NULL DEFAULT '',
    step_id         TEXT NULL,
    step_label      TEXT NULL,
    is_manual       BOOLEAN NULL,
    device_id       TEXT NULL,
    capability      TEXT NULL,
    parameters_json JSONB NULL,
    result_json     JSONB NULL,
    timestamp       TIMESTAMPTZ NOT NULL,
    CONSTRAINT fk_experiment_events_experiment
        FOREIGN KEY (experiment_id)
        REFERENCES experiments(runspace_id)   -- NOTE: current schema key
        ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_experiment_events_experiment_id
    ON experiment_events(experiment_id, sequence_num);
```

> **Risk:** `experiments.runspace_id` is the current PK (Phase 12 promotes `experiment_id` as a separate identity). Either add an `experiment_id` column to `experiments` in this migration, or accept that the FK target is `runspace_id` for now and migrate in a future phase.

---

### `helix-desktop/src/types/agent.ts` *(modify)*

**Analog:** self — extend existing discriminated union of WebSocket frame types.

**Existing RunspaceEvent type** (`agent.ts`, lines 67–74):
```typescript
export interface RunspaceEvent {
  type: string;
  runspaceId: string;
  timestamp: string;
  estimatedDurationMs?: number;
  [key: string]: unknown;
}
```

**Add experiment-scoped fields to RunspaceEvent** (optional additions, not breaking):
```typescript
export interface RunspaceEvent {
  type: string;
  runspaceId: string;
  timestamp: string;
  estimatedDurationMs?: number;
  experimentId?: string;       // NEW — present on experiment-scoped events
  experimentPhase?: string;    // NEW — "pre" | "main" | "post"
  [key: string]: unknown;
}
```

**Add new concrete event interfaces:**
```typescript
export interface ExperimentStartedEvent {
  type: "experiment-started";
  experimentId: string;
  experimentPhase: string;
  timestamp: string;
}

export interface ExperimentPhaseChangedEvent {
  type: "experiment-phase-changed";
  experimentId: string;
  experimentPhase: string;
  timestamp: string;
}

export interface ExperimentCompletedEvent {
  type: "experiment-completed";
  experimentId: string;
  timestamp: string;
}

export interface ManualActionEvent {
  type: "manual-action";
  deviceId: string;
  capability: string;
  parameters: Record<string, unknown>;
  success: boolean;
  result?: unknown;
  error?: string | null;
  experimentId?: string;       // NEW
  experimentPhase?: string;    // NEW
  timestamp: string;
}
```

---

### `helix-desktop/src/hooks/useAgentWebSocket.ts` *(modify)*

**Analog:** self — add experiment event routing alongside existing branches.

**Existing dispatch pattern** (`useAgentWebSocket.ts`, lines 25–41):
```typescript
ws.onmessage = (msg) => {
  try {
    const frame = JSON.parse(msg.data as string);
    if (frame.type === "trend-value") {
      setTrendValues((prev) => [...prev.slice(-200), frame as TrendValue]);
    } else if (frame.type === "safety-event") {
      setSafetyEvents((prev) => [...prev.slice(-50), frame as SafetyEvent]);
    } else if (frame.type === "device-status") {
      setDeviceStatusEvents((prev) => [...prev.slice(-50), frame as DeviceStatusEvent]);
    } else {
      setRunspaceEvents((prev) => [...prev.slice(-100), frame as RunspaceEvent]);
    }
  } catch { /* ignore */ }
};
```

**Add experiment event branch** (insert before the final `else`):
```typescript
} else if (
  frame.type === "experiment-started" ||
  frame.type === "experiment-phase-changed" ||
  frame.type === "experiment-completed" ||
  frame.type === "experiment-failed"
) {
  setExperimentEvents((prev) => [...prev.slice(-50), frame as ExperimentEvent]);
} else if (frame.type === "manual-action") {
  // manual-action is also a RunspaceEvent — keep in runspaceEvents for timeline
  // but also append to experimentEvents if experiment-scoped
  setRunspaceEvents((prev) => [...prev.slice(-100), frame as RunspaceEvent]);
  if (frame.experimentId) {
    setExperimentEvents((prev) => [...prev.slice(-50), frame as ExperimentEvent]);
  }
} else {
  setRunspaceEvents(...)
}
```

**Also add `experimentId` / `activePhase` to `AgentWebSocketState`:**
```typescript
export interface AgentWebSocketState {
  runspaceEvents: RunspaceEvent[];
  trendValues: TrendValue[];
  safetyEvents: SafetyEvent[];
  deviceStatusEvents: DeviceStatusEvent[];
  experimentEvents: ExperimentEvent[];   // NEW
  activeExperimentId: string | null;     // NEW — derived from last experiment-started
  activePhase: string | null;            // NEW — "pre"|"main"|"post"|null
  connected: boolean;
}
```

---

### `helix-desktop/src/hooks/useExperiment.ts` (hook, request-response)

**Analog:** `helix-desktop/src/hooks/useAgentHealth.ts`

**Pattern** (`useAgentHealth.ts`):
```typescript
export function useAgentHealth(baseUrl: string): AgentHealthState {
  const [healthy, setHealthy] = useState(false);
  useEffect(() => {
    const poll = async () => {
      const res = await fetch(`${baseUrl}/health`);
      setHealthy(res.ok && (await res.json()).status === "Healthy");
    };
    poll();
    const interval = setInterval(poll, 5000);
    return () => clearInterval(interval);
  }, [baseUrl]);
  return { healthy };
}
```

**New `useExperiment` hook shape** (I1, I6):
```typescript
export interface UseExperimentResult {
  experimentId: string | null;
  phase: ExperimentPhase | null;
  startExperiment: (name?: string) => Promise<string>;
  setPhase: (phase: ExperimentPhase) => Promise<void>;
  stopExperiment: () => Promise<void>;
  isRunning: boolean;
}

export function useExperiment(baseUrl: string): UseExperimentResult {
  const [experimentId, setExperimentId] = useState<string | null>(null);
  const [phase, setPhase] = useState<ExperimentPhase | null>(null);

  const startExperiment = async (name?: string) => {
    const res = await fetch(`${baseUrl}/experiments`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: name ?? "Experiment" }),
    });
    const data = await res.json();
    setExperimentId(data.experimentId);
    setPhase("pre");
    return data.experimentId;
  };
  // ...
}
```

---

### `helix-desktop/src/shell/panels/ExperimentTimeline.tsx` (component, event-driven)

**Analog:** `helix-desktop/src/shell/panels/LogOutput.tsx`

**LogOutput structure** (`LogOutput.tsx`, lines 1–40):
```typescript
import type { RunspaceEvent, SafetyEvent } from '../../types/agent';
import './panels.css';

export interface LogOutputProps {
  runspaceEvents: RunspaceEvent[];
  safetyEvents: SafetyEvent[];
}

export function LogOutput({ runspaceEvents, safetyEvents }: LogOutputProps) {
  return (
    <section aria-label="Log Output" data-testid="log-output">
      <ul className="log-list">
        {runspaceEvents.map((e, i) => (
          <li key={i} className="log-entry log-entry--runspace"
              data-testid={`log-runspace-${e.type}`}>
            {e.timestamp} {e.type}
          </li>
        ))}
        {/* safety events ... */}
      </ul>
    </section>
  );
}
```

**ExperimentTimeline shape** (I7):
```typescript
export interface ExperimentTimelineProps {
  runspaceEvents: RunspaceEvent[];    // carries experiment-scoped step events
  experimentEvents: ExperimentEvent[]; // lifecycle + phase changes
  activeExperimentId: string | null;
}

export function ExperimentTimeline({ runspaceEvents, experimentEvents, activeExperimentId }: ExperimentTimelineProps) {
  // Merge and sort by timestamp; group visually by phase scope
  // Mark manual-action events with a distinct icon/color
  return (
    <section aria-label="Experiment Timeline" data-testid="experiment-timeline">
      {/* phase scope headers (pre / main / post) with timestamps */}
      {/* step events nested under their phase */}
      {/* manual-action events with [M] marker per I3 */}
    </section>
  );
}
```

**Tab registration** mirrors how `output` tab is registered in `WorkspaceLayout.tsx` (lines 195–207):
```typescript
'experiment-timeline': (): TabData => ({
  id: 'experiment-timeline',
  title: 'Timeline',
  content: (
    <ExperimentTimeline
      runspaceEvents={props.runspaceEvents}
      experimentEvents={props.experimentEvents}
      activeExperimentId={props.activeExperimentId}
    />
  ),
  group: 'main',
  closable: false,
}),
```

And the `useEffect` that calls `updateTab` (WorkspaceLayout.tsx, lines 242–250) must include `experiment-timeline`.

---

### `helix-desktop/src/shell/HelixShell.tsx` + `Ribbon.tsx` *(modify)* (I6)

**Current Ribbon execution controls** (`Ribbon.tsx`, lines 42–75):
```typescript
onRunWorkflow: () => void;
onPauseWorkflow: () => void;
onAbortWorkflow: () => void;
// ...
<button onClick={onRunWorkflow} disabled={isRunning}>Run</button>
<button onClick={onPauseWorkflow} disabled={!isRunning || isPaused}>Pause</button>
<button onClick={onAbortWorkflow} disabled={!isRunning}>Abort</button>
```

**Current HelixShell handlers** (`HelixShell.tsx`, lines 62–72):
```typescript
const handleRun = async () => {
  await fetch(`${baseUrl}/api/runspaces/start`, { method: 'POST', ... });
};
const handlePause = async () => {
  await fetch(`${baseUrl}/api/runspaces/pause`, { method: 'POST' });
};
```

**Required changes for I6** — redirect handlers through the `useExperiment` hook:
```typescript
// In HelixShell.tsx
const experiment = useExperiment(baseUrl);

const handleRun = async () => {
  await experiment.startExperiment(currentWorkflow?.name);
};
const handlePause = async () => {
  // Pause currently-running runspace(s) under the active experiment
  if (runspaceId) await fetch(`${baseUrl}/runspaces/${runspaceId}/pause`, ...);
};
const handleAbort = async () => {
  await experiment.stopExperiment();
};
```

**Ribbon prop additions** (`Ribbon.tsx`):
- Add `experimentId?: string | null` and `activePhase?: string | null` props for display
- Add phase selector buttons to the "Execution" group: `[Pre] [Main] [Post]` buttons that call `onSetPhase`
- `isRunning` / `isPaused` continue to drive button disabled states but derived from experiment state not runspace state

---

## Shared Patterns

### Event emission in C# (IRunspaceEventEmitter.Publish)

**Source:** `Helix.Orchestration.Agent/Events/IRunspaceEventEmitter.cs` (lines 1–19) and `RunspaceEventEmitter.cs`

**Apply to:** `ExperimentEndpoints.cs`, `ExperimentManager.cs`

```csharp
// IRunspaceEventEmitter injected via DI
emitter?.Publish(new ExperimentStartedEvent(experimentId, "pre"));
emitter?.Publish(new ExperimentPhaseChangedEvent(experimentId, "main"));
```

The channel is unbounded and `Publish` is fire-and-forget — safe to call from endpoint handlers.

### REST error wrapping (try/catch → Results.Problem)

**Source:** `RunspaceEndpoints.cs` lines 30–38; `DeviceEndpoints.cs` lines 123–129

**Apply to:** All handlers in `ExperimentEndpoints.cs`

```csharp
catch (Exception ex)
{
    return Results.Problem(
        title: "Failed to ...",
        detail: ex.Message,
        statusCode: 500);
}
```

### Postgres upsert via Dapper

**Source:** `PostgresExperimentRepository.cs` lines 10–35; `PostgresCampaignRepository.cs` lines 10–56

**Apply to:** `PostgresExperimentEventRepository.cs` (use `INSERT ... ON CONFLICT` for upsert; use transaction for multi-row inserts as in `CampaignRepository`)

### DI registration extension method

**Source:** `InfrastructureServiceExtensions.cs` lines 8–20

**Apply to:** Add `IExperimentEventRepository` registration alongside existing experiment/campaign repos.

### rc-dock tab factory + `updateTab` refresh

**Source:** `WorkspaceLayout.tsx` lines 195–250

**Apply to:** `ExperimentTimeline` tab registration — follow the same `tabRegistry[id]()` factory pattern and add to the `useEffect` that calls `updateTab`.

---

## Gaps — No Close Analog (new code required)

| Gap | Role | Data Flow | Notes |
|---|---|---|---|
| `ExperimentPhase` scoping on runspaces (I2) | service | CRUD | No concept of "experiment owns multiple runspaces by phase" exists. Need `IExperimentManager.AddRunspaceToPhase(experimentId, phase, runspaceId)` or equivalent. |
| Replay log query (I5) | service | batch | No existing ordered-event-log query. `IExperimentEventRepository.GetReplayLogAsync` queries `experiment_events ORDER BY sequence_num` — pure SQL, no existing analog. |
| `experiment_id` as independent identity (I1) | model | — | Current `experiments` table uses `runspace_id` as PK. Phase 12 needs `experiment_id` as a separate stable identifier. Either add `experiment_id TEXT UNIQUE` to the table or create a new `experiment_containers` table. |
| ExperimentTimeline scope-nesting UI (I7) | component | event-driven | No existing grouped/nested event display. Closest is `LogOutput` flat list — timeline needs phase-grouped accordion or tree view. |
| Active experiment propagation to ManualControl (I3) | hook/component | event-driven | `DeviceManualControl.tsx` calls `POST /devices/{id}/execute` with no experiment context. Needs `experimentId` + `experimentPhase` injected via prop or React context. |

---

## Key Integration Points and Risks

### Risk 1: `experiments.runspace_id` PK vs `experimentId` identity

The current `experiments` table uses `runspace_id` as its primary key
(`postgres/migrations/001_initial.sql`, line 21; `IExperimentRepository.cs` `ExperimentRecord`).
Phase 12 requires `experimentId` to be the stable identity (one experiment can span multiple runspaces).

**Mitigation:** Migration 002 should add `experiment_id TEXT NOT NULL` to the `experiments` table with a new unique index, while keeping `runspace_id` as the technical PK for backward compatibility. The `IExperimentRepository` can be extended with `GetByExperimentIdAsync(experimentId)` without breaking existing callers.

### Risk 2: `WorkflowContext.Experiment` is optional and disconnected from runspace execution

`WorkflowContext.cs` (line 50) declares `IExperimentContext? Experiment { get; init; }` but `Program.cs` constructs `WorkflowContext` without setting it (the factory lambda at `Program.cs` lines 185–195 omits `Experiment`). Phase 12 needs the active `ExperimentContext` injected here so step execution events are emitted with `experimentId`.

**Mitigation:** The `RunspaceManager` factory in `Program.cs` must accept a parameter telling it which experiment a new runspace belongs to, or the `ExperimentEndpoints.cs` must store the mapping `experimentId → runspaceId` that lets the event recorder correlate them.

### Risk 3: WebSocket event ordering (experiment-started before step events)

`RunspaceEndpoints.cs` (lines 28–30) shows the established pattern:
```csharp
emitter?.Publish(new RunspaceStartedEvent(runspace.RunspaceId));  // published BEFORE StartAsync
await runspace.StartAsync(ct);
```
`ExperimentEndpoints.cs` must follow the same ordering — publish `ExperimentStartedEvent` before any runspace creation to guarantee correct event order on the WebSocket channel.

### Risk 4: `ManualActionEvent` has `RunspaceId = ""`

`ManualActionEvent` is deliberately not scoped to a runspace (comment in `RunspaceEvent.cs` line 93). When an experiment is active, the `POST /devices/{id}/execute` handler in `DeviceEndpoints.cs` (lines 82–120) must look up the active experiment and attach `experimentId`/`experimentPhase`. This requires either a scoped `IExperimentManager` dependency injection into `DeviceEndpoints` or a thread-local/ambient experiment context.

### Risk 5: `ExperimentRecord` in Infrastructure does not reference `IExperimentContext` from Core

`PostgresExperimentRepository` lives in `Helix.Orchestration.Infrastructure` and uses a plain `ExperimentRecord` record. `ExperimentContext.cs` lives in `Helix.Orchestration.Core`. They are not connected — the infrastructure layer does not depend on Core experiment abstractions.

**Pattern to follow:** `CampaignManager.cs` in SiteServer constructs `CampaignRecord` DTOs from its own domain model before calling the repository — same mapping layer is needed for `ExperimentContext → ExperimentRecord`.

---

## Metadata

**Analog search scope:** `./Helix.Orchestration.Core/`, `./Helix.Orchestration.Agent/`, `./Helix.Orchestration.Infrastructure/`, `./Helix.Orchestration.SiteServer/`, `./Helix.Orchestration.Recorder/`, `./helix-desktop/src/`, `./postgres/`

**Files scanned:** ~40 source files read directly; ~25 files via grep

**Pattern extraction date:** 2026-06-06

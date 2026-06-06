# Phase 12: Experiment as Top-Level Scope - Research

**Researched:** 2026-06-06
**Domain:** .NET 8 Minimal API orchestration / event persistence / React docking UI
**Confidence:** HIGH (all findings are from direct codebase inspection, not external research)

---

## Summary

Phase 12 promotes `ExperimentId` from a metadata tag on runspaces to the canonical top-level execution container. Every device command — whether issued by an automated workflow step or by a scientist using the Manual Control panel — must be captured under an `ExperimentId` so the full sequence can be queried and replayed.

The good news is that the codebase already has almost all the building blocks. `ExperimentContext`/`IExperimentContext` exist in Core with a lifecycle state machine (Created → Running → Suspended → Completed/Failed). `ExperimentRecord` exists in Infrastructure with a PostgreSQL repository. `ManualActionEvent`, `StepStartedEvent`, `StepCompletedEvent`, and the full `RunspaceEvent` hierarchy are already emitted on the WebSocket channel. `IRunspaceEventEmitter` is a singleton channel that all events flow through. The gaps are: (a) no `ExperimentId` field on any event, (b) no `POST /experiments` endpoint, (c) no `ExperimentEventLog` table for append-only replay, (d) `ExecutionPanel` calls runspace endpoints directly rather than experiment endpoints, and (e) no `ExperimentTimeline` dock in the frontend.

The recommended approach threads a thin `ExperimentId` string through the existing stack as a correlation field rather than a heavyweight restructuring of `RunspaceManager`. This minimises blast radius while satisfying all seven requirements.

**Primary recommendation:** Add `ExperimentId` as an optional correlation field on `RunspaceEvent` (base record), introduce an `ExperimentEventLog` append-only PostgreSQL table, expose a `POST /experiments` endpoint that creates the context and owns the runspace lifecycle, update `ManualActionEvent` to carry `ExperimentId`, and add an `ExperimentTimeline` dock that reads the existing WebSocket channel filtered by `experimentId`.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| ExperimentId generation / lifecycle state machine | API / Backend (Agent) | — | Authoritative source of truth; client must not generate IDs |
| Event emission (automated steps) | API / Backend (Agent) | — | Steps run inside RunspaceManager body, which already calls emitter |
| Event emission (manual actions) | API / Backend (Agent) | — | `POST /devices/{id}/execute` runs in Agent; emitter is injected there |
| Append-only event log persistence | Database / Storage (PostgreSQL) | — | Queryable replay requires ordered, durable storage |
| Experiment lifecycle controls (Start/Stop/Pause) | Frontend Server (Tauri/React) → API | — | UI sends commands to Agent experiment endpoint; Agent drives runspace |
| Experiment Timeline dock (live view) | Browser / Client (React) | WebSocket relay | Consumes existing WS channel, filters by experimentId |
| Pre/main/post scope tracking | API / Backend (Agent) | Database | Scope markers stored as event types in the event log |

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| I1 | Introduce ExperimentId as the top-level execution container; Agent exposes POST /experiments endpoint | New `ExperimentEndpoints.cs` following the pattern of `RunspaceEndpoints.cs`; `IExperimentManager` singleton analogous to `IRunspaceManager` |
| I2 | An experiment supports pre/main/post phase scopes as sibling execution scopes under the same experimentId | `ExperimentPhase` enum + scope-enter/scope-exit events in the event log; each phase maps to one `IRunspace` |
| I3 | Manual actions recorded inside the active experiment scope with identical event structure | Add `ExperimentId` field to `ManualActionEvent`; `POST /devices/{id}/execute` reads active experiment from `IExperimentManager` |
| I4 | Experiment scope tree persisted and queryable | New `experiment_events` PostgreSQL table; `IExperimentEventRepository` with append + query-by-id |
| I5 | Replay log: reconstruct full device command sequence given an experimentId | `GET /experiments/{id}/replay` queries `experiment_events` ordered by sequence_number |
| I6 | Start/Stop/Pause controls operate on experiment lifecycle | `ExecutionPanel` calls `POST /experiments` and `POST /experiments/{id}/pause` etc.; runspace lifecycle driven by experiment endpoint |
| I7 | Experiment Timeline dock shows live events with scope nesting, timestamps, manual action markers | New `ExperimentTimeline` React component; tab id `experiment-timeline` registered in `WorkspaceLayout.tsx` |
</phase_requirements>

---

## Design Decisions

### Decision 1: ExperimentId as Correlation Field on Runspace (Q1)

**Recommendation: Option C — add `ExperimentId` as a correlation field on events and on `IRunspace`.**

**Rationale from code inspection:**

`RunspaceManager` is a `ConcurrentDictionary<string, IRunspace>` factory. `IRunspace` has no concept of an owning experiment. Adding a new `IExperimentManager` that wraps `IRunspaceManager` is the cleanest path because:

1. `IRunspaceManager.CreateAsync` already takes a `RunspaceDefinition` and returns `IRunspace`. The experiment manager can call this after creating the experiment context.
2. `ExperimentRecord` in Infrastructure already exists but its primary key is `RunspaceId` (not `ExperimentId`). This must be updated to have `ExperimentId` as the primary key, with `RunspaceId` as a foreign/child field.
3. All `RunspaceEvent` subclasses have a `RunspaceId` positional property but no `ExperimentId`. Adding `ExperimentId` to the base record (`RunspaceEvent`) as a nullable string is the minimal change that lets the frontend filter by experiment.

**What changes:**
- `RunspaceEvent` base record gains `string? ExperimentId` parameter.
- All derived record constructors pass-through the value (the callers in `RunspaceManager` body and `DeviceEndpoints.cs` supply it from the active experiment context).
- `IRunspace` gains an optional `string? ExperimentId` property (can be set at construction time).
- `ExperimentRecord` gains an `ExperimentId` PK; `RunspaceId` becomes a child reference (one experiment → one or more runspaces in later phases; for Phase 12, one-to-one is fine).

**Why not Option A (ExperimentId wraps 1-N RunspaceIds)?**
This requires `IRunspaceManager` to know about experiments — a layering violation. RunspaceManager is already correctly ignorant of experiment semantics.

**Why not Option B (Runspace becomes child of Experiment at the data model level)?**
This is semantically correct for the long term but requires a larger database migration and a new foreign-key join in all existing runspace queries. Phase 12 can achieve the same traceability with a correlation field and upgrade the schema in a later phase.

---

### Decision 2: Pre/Main/Post Scopes (Q2)

**Recommendation: Option C — `ExperimentPhase` enum on the experiment; each phase maps to one Runspace.**

**Rationale:**

Option A (each scope is a separate Runspace) is the right long-term model because:
- `IRunspace` already has `StartedAt`/`EndedAt` timestamps — these become the scope entry/exit timestamps for free.
- Pre/main/post phases can run at different times (pre-experiment setup, then main run later, then post-teardown). Separate runspaces encode this cleanly.
- The `ExperimentPhase` enum (`Pre = 0, Main = 1, Post = 2`) lives on the experiment-level record and on each event, allowing the timeline to group events into swim lanes.

Option B (single runspace, internal scope markers) pollutes the runspace with experiment-level concerns and makes replay harder (cannot pause between phases).

**What changes:**
- New `ExperimentPhase` enum in Core: `Pre`, `Main`, `Post`.
- `ExperimentEndpoints.cs` creates a runspace per phase when the corresponding workflow definition is provided. For Phase 12, the initial `POST /experiments` body contains at most three optional `RunspaceDefinition` slots.
- `ScopeEnteredEvent(string ExperimentId, ExperimentPhase Phase, DateTimeOffset Timestamp)` and `ScopeExitedEvent` added to `RunspaceEvent.cs`. These are emitted when the experiment endpoint transitions the active phase.
- The `ExperimentEventLog` table stores `phase` as a column so `GET /experiments/{id}/replay` can return events grouped by phase.

---

### Decision 3: Event Persistence for Replay (Q3)

**Recommendation: Option A — append-only `experiment_events` table in PostgreSQL.**

**Rationale from code inspection:**

The codebase already uses Dapper against PostgreSQL for `experiments` and `campaigns` tables. Adding a third table follows the same pattern exactly. The `PostgresExperimentRepository` uses upsert (`ON CONFLICT ... DO UPDATE`) — the event log must use INSERT-only (no update, no delete) to guarantee audit integrity.

The `IObjectStorageSink` / MinIO path exists in Infrastructure but is used for object blobs, not structured event logs. Mixing JSONL files with PostgreSQL creates two query surfaces for replay. Keep it simple: PostgreSQL only.

**Schema for `experiment_events`:**
```sql
CREATE TABLE experiment_events (
    id              BIGSERIAL PRIMARY KEY,
    experiment_id   TEXT        NOT NULL,
    sequence_number BIGINT      NOT NULL,   -- monotonic within an experiment
    phase           TEXT        NOT NULL,   -- "pre" | "main" | "post" | "none"
    event_type      TEXT        NOT NULL,   -- mirrors RunspaceEvent.Type discriminator
    runspace_id     TEXT,
    step_id         TEXT,
    device_id       TEXT,
    capability      TEXT,
    payload         JSONB       NOT NULL,
    occurred_at     TIMESTAMPTZ NOT NULL,
    UNIQUE (experiment_id, sequence_number)
);
CREATE INDEX ix_experiment_events_experiment_id ON experiment_events (experiment_id, sequence_number);
```

**`IExperimentEventRepository`** (new interface in Infrastructure):
```csharp
Task AppendAsync(ExperimentEventRecord record, CancellationToken ct = default);
Task<IReadOnlyList<ExperimentEventRecord>> GetReplayLogAsync(string experimentId, CancellationToken ct = default);
Task<long> NextSequenceAsync(string experimentId, CancellationToken ct = default);
```

The `AgentService` broadcast loop (or a new `ExperimentEventPersistenceWorker` hosted service) subscribes to the `IRunspaceEventEmitter` channel and writes events that carry a non-null `ExperimentId` to the event log. This keeps persistence decoupled from the hot event-emission path.

**Why not JSONL?**
JSONL files on disk work for local archival but cannot support the queryable scope tree required by I4, and they require filesystem access that the Agent's Docker container does not guarantee across restarts.

**Why not both?**
Adding MinIO JSONL output can be a Phase 17 enhancement (trend archival). Phase 12 needs only the queryable database path.

---

### Decision 4: POST /experiments Endpoint Design (Q4)

**Recommendation: Accept an `ExperimentStartRequest` with optional per-phase `RunspaceDefinition` slots.**

**Rationale:**

`POST /runspaces` accepts a `RunspaceDefinition` directly and immediately starts execution. `POST /experiments` must do more: create an `ExperimentContext`, register the experiment in the DB, optionally start the pre-phase runspace, and return an `experimentId`. It should not immediately execute all phases in sequence — the scientist may want to pause between phases or add manual steps.

**Request shape:**
```csharp
public sealed record ExperimentStartRequest(
    string Name,
    RunspaceDefinition? PrePhase,
    RunspaceDefinition? MainPhase,
    RunspaceDefinition? PostPhase,
    Dictionary<string, object?>? Metadata
);
```

**Response shape:**
```json
{
  "experimentId": "abc123",
  "name": "My Experiment",
  "lifecycle": "Running",
  "activePhase": "main",
  "activeRunspaceId": "xyz789"
}
```

**Additional endpoints:**
- `GET /experiments/{id}` — returns lifecycle, active phase, timestamps
- `POST /experiments/{id}/pause` — pauses the active runspace, transitions experiment to Suspended
- `POST /experiments/{id}/resume` — resumes the active runspace
- `POST /experiments/{id}/abort` — aborts active runspace, transitions experiment to Failed
- `GET /experiments/{id}/replay` — returns ordered `experiment_events` for the experiment
- `GET /experiments` — list all experiments (for history panel)

**Difference from `POST /workflows/run`:**
`POST /workflows/run` does not exist — `POST /runspaces` is the current execution entry point. `POST /experiments` owns the full lifecycle: it creates the experiment context, kicks off the pre-phase runspace (if provided), and transitions through phases as each runspace completes.

**Phase sequencing strategy:** Automatic sequential advancement (pre → main → post) is opt-in via a `autoAdvance: true` flag on the request. Default is manual advancement so scientists can insert manual actions between phases.

---

### Decision 5: Manual Action Recording (Q3/I3)

**Recommendation: Option A — execution endpoint reads active experiment from `IExperimentManager` and enriches the emitted `ManualActionEvent`.**

**Rationale from code inspection:**

`POST /devices/{id}/execute` in `DeviceEndpoints.cs` already injects `IRunspaceEventEmitter` and emits `ManualActionEvent`. The minimal change is:
1. Inject `IExperimentManager` into `DeviceEndpoints.MapDeviceEndpoints`.
2. Call `experimentManager.GetActive()` to retrieve the current `ExperimentContext` (if any).
3. Pass `experimentId` and `phase` to the `ManualActionEvent` constructor.

`ManualActionEvent` currently has `RunspaceId = ""`. Add `ExperimentId` and `Phase` fields.

**Why not a new `/experiments/{id}/manual-action` endpoint?**
It adds an extra round-trip and requires the client to know the active experiment ID before issuing every manual command. The active-experiment-from-context pattern is simpler: the Agent tracks the active experiment as a singleton (one experiment active at a time per agent process), and `POST /devices/{id}/execute` automatically tags the event.

**IExperimentManager.GetActive():**
```csharp
public interface IExperimentManager
{
    Task<IExperimentContext?> GetActiveAsync();
    Task<IExperimentContext> StartAsync(ExperimentStartRequest request, CancellationToken ct = default);
    Task PauseAsync(string experimentId, CancellationToken ct = default);
    Task ResumeAsync(string experimentId, CancellationToken ct = default);
    Task AbortAsync(string experimentId, CancellationToken ct = default);
    IExperimentContext? Get(string experimentId);
}
```

The singleton active-experiment pointer (`_activeExperimentId`) is set when `POST /experiments` is called and cleared when the experiment reaches a terminal state. For Phase 12, only one experiment is active at a time; multi-experiment concurrency is a Phase 17+ concern.

---

### Decision 6: helix-desktop Experiment Lifecycle Controls (Q6)

**Recommendation: Option A — `ExecutionPanel` calls `POST /experiments` and experiment lifecycle endpoints; it does not call runspace endpoints directly.**

**Rationale from code inspection:**

`ExecutionPanel.tsx` currently:
1. Calls `POST /workflows/compile` → gets a `RunspaceDefinition`
2. Posts the definition to `POST /runspaces` → gets `runspaceId`
3. Calls `POST /runspaces/{id}/pause`, `/resume`, `/abort` via `runspaceId`

The migration path for Phase 12:
1. `handleStart` posts the compiled `RunspaceDefinition` as `MainPhase` in an `ExperimentStartRequest` to `POST /experiments` → gets back `experimentId`.
2. `handlePause` / `handleResume` / `handleAbort` call `POST /experiments/{id}/pause` etc.
3. `ExecutionPanel` stores `experimentId` in state instead of (or in addition to) `runspaceId`.
4. `runspaceEvents` are still used for step-level progress, but filtered by `experimentId` (using the new field on `RunspaceEvent`).

**Why not a new `ExperimentPanel` component?**
`ExecutionPanel` already has the correct structure and tests (`ExecutionPanel.test.tsx`). Enriching it to call experiment endpoints is less churn than creating a parallel component and migrating the dock registration in `WorkspaceLayout.tsx`.

**Test compatibility:** The existing `ExecutionPanel.test.tsx` tests wire against `POST /runspaces`. Those test fixtures must be updated to mock `POST /experiments` instead. The test file must be updated as part of the implementation.

---

### Decision 7: Experiment Timeline Dock (Q7)

**Recommendation: New `ExperimentTimeline` component registered as tab id `experiment-timeline` in `WorkspaceLayout.tsx`.**

**WebSocket events consumed:**
The timeline component subscribes to the same `runspaceEvents` array already passed to `ExecutionPanel`, but filters by `experimentId`. It also consumes the new `ScopeEnteredEvent` and `ScopeExitedEvent` types (added in Decision 2).

**Event types the timeline renders:**
| Event Type | Visual Treatment |
|------------|-----------------|
| `experiment-started` | Green header row: "Experiment ABC123 started" |
| `scope-entered` | Indented section header: "Pre / Main / Post" with entry timestamp |
| `step-started` | Indented row with ETA badge |
| `step-completed` | Row updated with green check, elapsed ms |
| `step-faulted` | Row updated with red X, fault message |
| `manual-action` | Indented row with hand icon, device + capability name, params |
| `scope-exited` | Indented section footer with exit timestamp and duration |
| `experiment-completed` | Green footer row |
| `experiment-aborted` | Grey footer row |
| `experiment-faulted` | Red footer row |

**Scope nesting rendered as indented tree:**
```
[experiment-started]  ABC123 — My Experiment
  [scope-entered]     ▶ Main Phase  12:00:01.000
    [step-started]      step-1 — SetFlow(100 mL/min)  ~2s
    [step-completed]    step-1   ✓  1.8s
    [manual-action]   ✋ simulator.SetTemperature(25°C)  ✓
    [step-started]      step-2 — Delay(60s)  ~60s
    [step-completed]    step-2   ✓  60.1s
  [scope-exited]      ■ Main Phase  12:01:02.123  (61.1s)
[experiment-completed] ABC123  ✓
```

**New agent WebSocket event types (additions to `RunspaceEvent.cs`):**
```csharp
public sealed record ExperimentStartedEvent(string ExperimentId, string Name)
    : RunspaceEvent("experiment-started", "", DateTimeOffset.UtcNow) { ... }

public sealed record ScopeEnteredEvent(string ExperimentId, string Phase)
    : RunspaceEvent("scope-entered", "", DateTimeOffset.UtcNow) { ... }

public sealed record ScopeExitedEvent(string ExperimentId, string Phase, TimeSpan Duration)
    : RunspaceEvent("scope-exited", "", DateTimeOffset.UtcNow) { ... }

public sealed record ExperimentCompletedEvent(string ExperimentId)
    : RunspaceEvent("experiment-completed", "", DateTimeOffset.UtcNow) { ... }

public sealed record ExperimentAbortedEvent(string ExperimentId)
    : RunspaceEvent("experiment-aborted", "", DateTimeOffset.UtcNow) { ... }

public sealed record ExperimentFaultedEvent(string ExperimentId, string FaultMessage)
    : RunspaceEvent("experiment-faulted", "", DateTimeOffset.UtcNow) { ... }
```

**Frontend type additions to `agent.ts`:**
```typescript
export interface ExperimentStartedEvent {
  type: "experiment-started";
  experimentId: string;
  name: string;
  timestamp: string;
}
export interface ScopeEnteredEvent {
  type: "scope-entered";
  experimentId: string;
  phase: "pre" | "main" | "post";
  timestamp: string;
}
// ... etc
```

**`useAgentWebSocket` hook update:** Add `experimentEvents: ExperimentEvent[]` to `AgentWebSocketState` and dispatch to it for `type.startsWith("experiment-")` or `scope-entered` / `scope-exited`.

**WorkspaceLayout.tsx registration:** Add `'experiment-timeline'` to the `tabRegistry` map and add `{ id: 'experiment-timeline' }` to `panel-main` tabs in `DEFAULT_LAYOUT`.

---

## Integration Points with Existing Code

### 1. `RunspaceEvent` base record — add `ExperimentId`
**File:** `Helix.Orchestration.Agent/Events/RunspaceEvent.cs`
**Change:** Add `string? ExperimentId = null` to the positional record. All 12 existing derived records compile without change (positional parameter defaults).
**Risk:** LOW — C# positional record with default value is backward compatible.

### 2. `ManualActionEvent` — add `ExperimentId` and `Phase`
**File:** `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` (same file)
**Change:** Add `string? ExperimentId` and `string? Phase` properties.
**Caller:** `DeviceEndpoints.cs` line 152 — update the `ManualActionEvent` constructor call to pass active experiment context.

### 3. `ExperimentRecord` — add `ExperimentId` PK
**File:** `Helix.Orchestration.Infrastructure/Persistence/IExperimentRepository.cs`
**Current state:** `ExperimentRecord` uses `RunspaceId` as its primary key (both in C# and SQL `ON CONFLICT (runspace_id)`).
**Change:** Add `ExperimentId` string field; update SQL schema to use `experiment_id` as PK. `RunspaceId` becomes nullable child field.
**Risk:** MEDIUM — requires a database migration. The `experiments` table structure changes. Existing data in development environments must be migrated or dropped.

### 4. `IExperimentManager` — new singleton service
**File:** New `Helix.Orchestration.Agent/Experiments/IExperimentManager.cs` and `ExperimentManager.cs`
**Registered in:** `Program.cs` as `builder.Services.AddSingleton<IExperimentManager, ExperimentManager>()`
**Relationship to `IRunspaceManager`:** `ExperimentManager` calls `IRunspaceManager.CreateAsync` when starting a phase; it does not replace `IRunspaceManager`.

### 5. `ExperimentEndpoints.cs` — new endpoint file
**File:** New `Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs`
**Pattern:** Identical to `RunspaceEndpoints.cs` — static class, `MapExperimentEndpoints(this WebApplication app)` extension method, called from `Program.cs`.
**Endpoints:** POST /experiments, GET /experiments, GET /experiments/{id}, POST /experiments/{id}/pause, POST /experiments/{id}/resume, POST /experiments/{id}/abort, GET /experiments/{id}/replay.

### 6. `ExperimentEventPersistenceWorker` — new hosted service
**File:** New `Helix.Orchestration.Agent/Experiments/ExperimentEventPersistenceWorker.cs`
**Purpose:** Subscribes to `IRunspaceEventEmitter.Events`, writes events with non-null `ExperimentId` to `IExperimentEventRepository`.
**Risk:** The emitter channel is `UnboundedChannel`. If the DB write falls behind, events queue up in memory. For Phase 12, this is acceptable. Phase 19 hardening can add back-pressure.

### 7. `WorkspaceLayout.tsx` — register ExperimentTimeline tab
**File:** `helix-desktop/src/shell/WorkspaceLayout.tsx`
**Changes:** 
- Import `ExperimentTimeline` component
- Add `'experiment-timeline'` entry to `tabRegistry`
- Add `{ id: 'experiment-timeline' }` to `panel-main` tabs in `DEFAULT_LAYOUT`
- Add `dockRef.current.updateTab('experiment-timeline', ...)` in `useEffect` for live updates

### 8. `ExecutionPanel.tsx` — migrate to experiment endpoints
**File:** `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx`
**Changes:**
- `handleStart`: after compile, POST to `/experiments` instead of `/runspaces`; store `experimentId` from response
- `handlePause/Resume/Abort`: call `/experiments/{id}/pause` etc.
- Add `experimentId` to component state
- Filter `runspaceEvents` by `experimentId` (new field)

### 9. `useAgentWebSocket.ts` — dispatch experiment events
**File:** `helix-desktop/src/hooks/useAgentWebSocket.ts`
**Changes:** Add `experimentEvents` state array; dispatch frames with type `experiment-started`, `experiment-completed`, `experiment-faulted`, `experiment-aborted`, `scope-entered`, `scope-exited` to it.

---

## Architecture Patterns

### System Architecture Diagram

```
helix-desktop
┌─────────────────────────────────────────────────────────┐
│  ExperimentTimeline dock  ←── experimentEvents (WS)     │
│  ExecutionPanel           ──→ POST /experiments          │
│                           ←── experimentId              │
│                           ──→ POST /experiments/{id}/pause│
│  DeviceManualControl      ──→ POST /devices/{id}/execute │
└──────────────┬──────────────────────────┬───────────────┘
               │ HTTP REST                │ WebSocket /ws
               ▼                          ▼
┌─────────────────────────────────────────────────────────┐
│  Agent (ASP.NET Core Minimal API)                       │
│                                                         │
│  ExperimentEndpoints ──→ IExperimentManager             │
│                              │                          │
│                    ┌─────────┴──────────┐               │
│                    ▼                    ▼               │
│             IRunspaceManager    IExperimentEventRepository│
│                    │                    │               │
│            RunspaceManager     ExperimentEventPersistenceWorker
│                    │                    ▲               │
│            [RunspaceBody]      IRunspaceEventEmitter    │
│                    │                    ▲               │
│            StepStartedEvent ───────────┘               │
│                                                         │
│  DeviceEndpoints ──→ IRunspaceEventEmitter              │
│                  ──→ IExperimentManager.GetActive()     │
│              (enriches ManualActionEvent with expId)    │
│                                                         │
│  AgentService ──→ broadcasts all events over WS         │
└──────────────────────────┬──────────────────────────────┘
                           │ Dapper / PostgreSQL
                           ▼
┌─────────────────────────────────────────────────────────┐
│  PostgreSQL                                             │
│  ├── experiments          (lifecycle, metadata)         │
│  ├── experiment_events    (append-only event log)       │
│  └── campaigns            (existing, unchanged)         │
└─────────────────────────────────────────────────────────┘
```

### Recommended Project Structure (new files only)

```
Helix.Orchestration.Agent/
├── Api/
│   └── ExperimentEndpoints.cs        # POST/GET /experiments, replay endpoint
├── Experiments/
│   ├── IExperimentManager.cs         # Start/Pause/Resume/Abort/GetActive
│   └── ExperimentManager.cs          # Singleton; wraps IRunspaceManager
│   └── ExperimentEventPersistenceWorker.cs  # IHostedService; drains emitter → DB

Helix.Orchestration.Core/Experiment/
│   └── ExperimentPhase.cs            # enum Pre / Main / Post

Helix.Orchestration.Infrastructure/Persistence/
│   ├── IExperimentEventRepository.cs # Append + replay query
│   └── PostgresExperimentEventRepository.cs

helix-desktop/src/
├── components/ExperimentTimeline/
│   ├── ExperimentTimeline.tsx
│   └── ExperimentTimeline.test.tsx
```

### Pattern: Append-Only Event Log via Hosted Service

```csharp
// Source: codebase — mirrors AgentService.BroadcastLoopAsync pattern
public sealed class ExperimentEventPersistenceWorker(
    IRunspaceEventEmitter emitter,
    IServiceScopeFactory scopeFactory,
    ILogger<ExperimentEventPersistenceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in emitter.Events.ReadAllAsync(stoppingToken))
        {
            if (string.IsNullOrEmpty(evt.ExperimentId)) continue;

            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExperimentEventRepository>();
            var seq  = await repo.NextSequenceAsync(evt.ExperimentId, stoppingToken);

            await repo.AppendAsync(new ExperimentEventRecord(
                ExperimentId:   evt.ExperimentId,
                SequenceNumber: seq,
                Phase:          evt.Phase ?? "none",
                EventType:      evt.Type,
                RunspaceId:     evt.RunspaceId.Length > 0 ? evt.RunspaceId : null,
                Payload:        JsonSerializer.Serialize(evt),
                OccurredAt:     evt.Timestamp
            ), stoppingToken);
        }
    }
}
```

**Note on scope:** `IExperimentEventRepository` will be registered as `Scoped` (following the existing `IExperimentRepository` pattern). The hosted service must create a scope per event to avoid holding a scoped service for the lifetime of the worker.

### Pattern: Active Experiment Correlation in Device Endpoint

```csharp
// Source: codebase — DeviceEndpoints.cs line 152, enriched
emitter?.Publish(new ManualActionEvent(
    DeviceId:     id,
    Capability:   request.Capability,
    Parameters:   request.Parameters ?? new(),
    Success:      invocationResult.Success,
    Result:       invocationResult.ReturnValue,
    Error:        invocationResult.ErrorMessage,
    ExperimentId: (await experimentManager.GetActiveAsync())?.ExperimentId,
    Phase:        (await experimentManager.GetActiveAsync())?.ActivePhase?.ToString()?.ToLowerInvariant()
));
```

### Pattern: ExperimentTimeline React component (event filtering)

```typescript
// Source: codebase — mirrors useAgentWebSocket dispatch pattern
const experimentEvents = runspaceEvents.filter(
  (e) => e.experimentId === activeExperimentId
);
const scopeEnteredEvents = experimentEvents.filter(
  (e) => e.type === "scope-entered"
);
```

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Sequence numbers for event ordering | Custom auto-increment in application code | PostgreSQL `BIGSERIAL` or `SELECT COALESCE(MAX(seq),0)+1 FOR UPDATE` | Race-condition-free under concurrent inserts |
| Event channel back-pressure | Custom bounded queue | `Channel.CreateBounded<RunspaceEvent>` with `BoundedChannelFullMode.DropOldest` | Already in `System.Threading.Channels`; `ExperimentEventPersistenceWorker` can switch from unbounded to bounded if DB write latency becomes a problem |
| ExperimentId generation | Custom slug/timestamp ID | `Guid.NewGuid().ToString("N")` (already used for `RunspaceId` in `DurableRunspace.cs`) | Consistent with existing pattern |
| Scope-enter/exit timing | Manual `DateTimeOffset.UtcNow` calls in endpoint handlers | Emit `ScopeEnteredEvent` and `ScopeExitedEvent` from `IExperimentManager` state transitions | Keeps timing logic in one place; the emitter's Timestamp is authoritative |
| Active experiment singleton | Thread-safe custom lock | `Interlocked.Exchange` on an `IExperimentContext?` reference in `ExperimentManager` | Already used in `DurableRunspace.cs` gate pattern |

---

## Common Pitfalls

### Pitfall 1: Channel Reader Duplication
**What goes wrong:** `AgentService.BroadcastLoopAsync` already reads from `emitter.Events`. A second reader (the persistence worker) on the same `ChannelReader` will not receive events — `Channel<T>` is not a multicast source. Only one reader consumes each item.
**Why it happens:** `RunspaceEventEmitter` wraps a single `Channel<RunspaceEvent>`. The existing `AgentService` consumes all events for broadcast.
**How to avoid:** The `ExperimentEventPersistenceWorker` must NOT read from `emitter.Events` directly. Instead, `AgentService.BroadcastLoopAsync` should forward experiment-scoped events to a second, dedicated `IExperimentEventChannel` (a new `Channel<RunspaceEvent>` registered as a singleton). The persistence worker reads from the experiment channel. Alternatively, `RunspaceEventEmitter` can become a multicast source using a `ConcurrentDictionary<string, Channel<RunspaceEvent>>` of subscriber channels.

**Recommended approach:** Add a `IExperimentEventChannel` wrapper (same pattern as `IRunspaceEventEmitter`) and have `AgentService` forward events with non-null `ExperimentId` to it:
```csharp
// In BroadcastLoopAsync, after serializing + sending to WS clients:
if (evt.ExperimentId is not null)
    experimentChannel.Publish(evt);
```

**Warning signs:** Persistence worker starts but no events appear in `experiment_events` table despite experiment running.

### Pitfall 2: `ExperimentRecord` Primary Key Migration
**What goes wrong:** `PostgresExperimentRepository` uses `ON CONFLICT (runspace_id) DO UPDATE`. The existing `experiments` table has `runspace_id` as PK. If we add `experiment_id` as a new column without a migration, `GetByRunspaceIdAsync` and `GetByCampaignIdAsync` continue working but `GET /experiments/{experimentId}` has no matching column to query against.
**How to avoid:** Write a Dapper migration script that:
1. Adds `experiment_id TEXT` column to `experiments`.
2. Backfills `experiment_id = runspace_id` for all existing rows.
3. Adds `NOT NULL` constraint.
4. Creates unique index on `experiment_id`.
Include this as a Wave 0 task that runs before any code that writes `experiment_id`.

### Pitfall 3: `ExecutionPanel` Tests Still Mock `/runspaces`
**What goes wrong:** After migrating `ExecutionPanel.handleStart` to call `/experiments`, the existing `ExecutionPanel.test.tsx` mocks (`vi.spyOn(fetch)` returning `{ runspaceId: "..." }`) will fail because the response shape changes to `{ experimentId: "...", activeRunspaceId: "..." }`.
**How to avoid:** Update mock fixtures in `ExecutionPanel.test.tsx` alongside the component changes. Do not defer the test update — the CI build will catch it if left stale.

### Pitfall 4: `ManualActionEvent` ExperimentId Race
**What goes wrong:** `POST /devices/{id}/execute` calls `experimentManager.GetActiveAsync()` twice (or once but using the result after an await). If the experiment transitions to Completed between the two calls, `ExperimentId` on the event may be null or stale.
**How to avoid:** Capture `activeContext = await experimentManager.GetActiveAsync()` once at the start of the endpoint handler, before invoking the capability. Use the captured reference for both the invocation result and the event.

### Pitfall 5: rc-dock `updateTab` for ExperimentTimeline
**What goes wrong:** `WorkspaceLayout.tsx` already uses `updateTab` in a `useEffect` to push updated `runspaceEvents` into the `execution-panel` and `output` tabs. Adding `experiment-timeline` to this effect is required, or the timeline will show stale event snapshots.
**How to avoid:** Add `dockRef.current.updateTab('experiment-timeline', tabRegistry['experiment-timeline'](), false)` to the same `useEffect` block. This is the established pattern — see the memory note `project_rc_dock_fbox.md` confirming rc-dock does not re-call `loadTab` when props change.

### Pitfall 6: `ExperimentLifecycle` vs `RunspaceStatus` Naming Confusion
**What goes wrong:** `ExperimentLifecycle` uses `Running/Suspended/Completed/Failed`; `RunspaceStatus` uses `Running/Paused/Completed/Faulted`. The mapping is not 1:1 (`Paused` → `Suspended`, `Faulted` → `Failed`).
**How to avoid:** `ExperimentManager` must translate: when the active runspace transitions to `Paused`, the experiment transitions to `Suspended`. Keep this translation explicit in `ExperimentManager.PauseAsync` — do not have runspace lifecycle changes automatically propagate to experiment lifecycle.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Vitest 3.x (frontend), xUnit 2.x (backend) |
| Config file | `helix-desktop/vite.config.ts` (includes vitest config), `*.Tests/*.csproj` |
| Quick run command | `cd helix-desktop && npm test -- --run` (frontend); `dotnet test --filter "Category=Unit"` (backend) |
| Full suite command | `cd helix-desktop && npm test -- --run` + `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| I1 | POST /experiments creates ExperimentId, returns 200 | unit (xUnit) | `dotnet test --filter "ExperimentEndpoints"` | ❌ Wave 0 |
| I2 | ScopeEnteredEvent emitted when phase starts | unit (xUnit) | `dotnet test --filter "ExperimentManager"` | ❌ Wave 0 |
| I3 | ManualActionEvent carries experimentId when experiment active | unit (xUnit) | `dotnet test --filter "DeviceEndpoints_Manual"` | ❌ Wave 0 |
| I4 | AppendAsync stores event; GetReplayLogAsync returns ordered list | unit (xUnit) | `dotnet test --filter "ExperimentEventRepository"` | ❌ Wave 0 |
| I5 | GET /experiments/{id}/replay returns events in sequence_number order | integration (xUnit) | `dotnet test --filter "ExperimentReplay"` | ❌ Wave 0 |
| I6 | ExecutionPanel calls /experiments not /runspaces | unit (Vitest) | `npm test -- --run ExecutionPanel` | ❌ update existing |
| I7 | ExperimentTimeline renders scope-entered events with nesting | unit (Vitest) | `npm test -- --run ExperimentTimeline` | ❌ Wave 0 |

### Wave 0 Gaps
- [ ] `Helix.Orchestration.Tests/ExperimentEndpointsTests.cs` — covers I1
- [ ] `Helix.Orchestration.Tests/ExperimentManagerTests.cs` — covers I2, I6
- [ ] `Helix.Orchestration.Tests/ExperimentEventRepositoryTests.cs` — covers I4, I5
- [ ] `helix-desktop/src/components/ExperimentTimeline/ExperimentTimeline.test.tsx` — covers I7
- [ ] Update `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.test.tsx` — update mocks for I6

---

## Suggested Wave Breakdown

### Wave 0: Core Data Model + DB Schema (backend)
**Goal:** Introduce `ExperimentId` correlation field; establish event log table. No behavior changes yet.

Tasks:
1. Add `string? ExperimentId` to `RunspaceEvent` base record (default null, backward compatible).
2. Add `ExperimentPhase` enum to Core.
3. Add `ScopeEnteredEvent`, `ScopeExitedEvent`, `ExperimentStartedEvent`, `ExperimentCompletedEvent`, `ExperimentAbortedEvent`, `ExperimentFaultedEvent` to `RunspaceEvent.cs`.
4. Write database migration: add `experiment_id` column to `experiments` table; create `experiment_events` table.
5. Create `IExperimentEventRepository` and `PostgresExperimentEventRepository` in Infrastructure.
6. Write repository unit tests (in-memory or against a test Postgres container).

### Wave 1: IExperimentManager + POST /experiments (backend)
**Goal:** Scientist can start an experiment via POST /experiments; the experiment context drives a runspace.

Tasks:
1. Create `IExperimentManager` and `ExperimentManager` (Start/Pause/Resume/Abort/GetActive).
2. Create `ExperimentEndpoints.cs` (all 7 endpoints).
3. Register `IExperimentManager`, `IExperimentEventRepository` in `Program.cs`.
4. Create `IExperimentEventChannel` wrapper; update `AgentService.BroadcastLoopAsync` to forward experiment events.
5. Create `ExperimentEventPersistenceWorker` hosted service.
6. Update `ManualActionEvent` with `ExperimentId` / `Phase`; update `DeviceEndpoints.cs` to enrich the event.
7. Write endpoint unit tests and `ExperimentManager` unit tests.

### Wave 2: helix-desktop Experiment Controls (frontend)
**Goal:** ExecutionPanel drives experiment lifecycle; experiment events visible in frontend.

Tasks:
1. Update `agent.ts` — add `ExperimentStartedEvent`, `ScopeEnteredEvent`, `ScopeExitedEvent`, `ExperimentCompletedEvent`, `ExperimentAbortedEvent`, `ExperimentFaultedEvent` types; add `experimentId` field to `RunspaceEvent`.
2. Update `useAgentWebSocket.ts` — add `experimentEvents` state; dispatch experiment-type frames.
3. Update `ExecutionPanel.tsx` — migrate `handleStart/Pause/Resume/Abort` to experiment endpoints; store `experimentId`.
4. Update `ExecutionPanel.test.tsx` — update mock fixtures.
5. Create `ExperimentTimeline` component (static rendering with test data first).
6. Register `experiment-timeline` tab in `WorkspaceLayout.tsx`.
7. Write `ExperimentTimeline.test.tsx`.

### Wave 3: Live Integration + Replay (full stack)
**Goal:** End-to-end: start experiment, see live timeline, query replay.

Tasks:
1. Wire `ExperimentTimeline` to live `experimentEvents` from `useAgentWebSocket`.
2. Update `updateTab` call in `WorkspaceLayout.tsx` `useEffect` to include `experiment-timeline`.
3. Implement `GET /experiments` list endpoint and a basic past-experiments panel (or extend `ExperimentTimeline` with a history dropdown).
4. Manual integration test: start experiment, issue manual device command, complete experiment, verify replay log is correct via `GET /experiments/{id}/replay`.
5. Update VERIFICATION.md with UAT steps for I1–I7.

---

## Security Domain

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No (Phase 19 concern) | — |
| V5 Input Validation | Yes | `ExperimentStartRequest` — validate `Name` is non-empty; `RunspaceDefinition` already validated by `WslCompiler` |
| V6 Cryptography | No | ExperimentId is a GUID, not a secret |

No new attack surface beyond what Phase 6 introduced with `POST /devices/{id}/execute`. Experiment endpoints follow the same CORS policy already configured in `Program.cs`.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | One experiment active at a time per Agent process is sufficient for Phase 12 | Decision 5, `IExperimentManager.GetActive()` | If scientists need concurrent experiments, the active-pointer singleton breaks; would need multi-experiment tracking |
| A2 | `ExperimentLifecycle.Suspended` maps to `RunspaceStatus.Paused` for UI labeling purposes | Pitfall 6 | UI shows wrong status if translation is not explicit |
| A3 | The `experiment_events` table will not grow beyond millions of rows in Phase 12's operational window (no archival strategy needed yet) | Decision 3 | If experiments run for days with sub-second events, table may need partitioning; deferred to Phase 17 |

---

## Open Questions

1. **Auto-advance between phases (pre → main → post)**
   - What we know: `ExperimentPhase` separates execution into three possible runspaces.
   - What's unclear: Should completion of the pre-phase automatically start the main phase, or always wait for operator confirmation?
   - Recommendation: Implement `autoAdvance: bool` on `ExperimentStartRequest` (default false). This is a data field, not a behavior decision for Phase 12 — the planner can default it to false and make it configurable.

2. **Single-workflow experiment (no pre/post phases)**
   - What we know: Most current workflows are a single `RunspaceDefinition` with no pre/post.
   - What's unclear: When the scientist clicks Start in `ExecutionPanel`, do they always get a three-phase experiment, or a single-phase "Main only" experiment?
   - Recommendation: `POST /experiments` should accept a degenerate case: `{ "name": "...", "mainPhase": <RunspaceDefinition> }` with no pre/post. This is the default path and is backward compatible with existing `ExecutionPanel` usage.

3. **Experiment naming UX**
   - What we know: `ExperimentContext` requires a `Name` parameter.
   - What's unclear: Does the UI provide a name dialog before starting, or auto-generate from workflow name + timestamp?
   - Recommendation: Default to `{workflowName}-{yyyy-MM-dd-HH-mm-ss}` auto-generated name; allow override in a modal that can be added in Phase 13 or later.

---

## Environment Availability

All dependencies are already present from Phase 11:

| Dependency | Required By | Available | Fallback |
|------------|------------|-----------|----------|
| PostgreSQL | experiment_events table | ✓ (existing DB from Phase 6) | — |
| Cleipnir PostgreSQL store | DurableRunspace | ✓ (already initialized in Program.cs) | — |
| rc-dock | ExperimentTimeline dock tab | ✓ (Phase 11) | — |
| Dapper | PostgresExperimentEventRepository | ✓ (existing dependency) | — |

---

## Sources

### Primary (HIGH confidence — direct codebase inspection)
- `Helix.Orchestration.Core/Experiment/ExperimentContext.cs` — lifecycle state machine, existing ExperimentId/Name/Lifecycle fields
- `Helix.Orchestration.Core/Experiment/IExperimentContext.cs` — interface contract
- `Helix.Orchestration.Core/Experiment/ExperimentLifecycle.cs` — Created/Running/Suspended/Completed/Failed states
- `Helix.Orchestration.Core/Runspace/IRunspace.cs` — existing runspace interface
- `Helix.Orchestration.Core/Runspace/RunspaceManager.cs` — ConcurrentDictionary factory pattern
- `Helix.Orchestration.Core/WorkflowEngine/WorkflowContext.cs` — confirms `IExperimentContext?` is already a field on WorkflowContext
- `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` — all 12 event types, `ManualActionEvent` structure
- `Helix.Orchestration.Agent/Events/RunspaceEventEmitter.cs` — single-reader unbounded channel
- `Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs` — endpoint pattern to replicate
- `Helix.Orchestration.Agent/Api/DeviceEndpoints.cs` — `ManualActionEvent` emission site
- `Helix.Orchestration.Agent/AgentService.cs` — `BroadcastLoopAsync`, single-reader channel confirmation
- `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs` — `IWorkflowExecutor` delegation, pause/resume pattern
- `Helix.Orchestration.Agent/Program.cs` — DI registrations, `IRunspaceManager` singleton factory
- `Helix.Orchestration.Infrastructure/Persistence/IExperimentRepository.cs` — current schema with `RunspaceId` as PK
- `Helix.Orchestration.Infrastructure/Persistence/PostgresExperimentRepository.cs` — Dapper upsert pattern
- `helix-desktop/src/types/agent.ts` — TypeScript event types
- `helix-desktop/src/hooks/useAgentWebSocket.ts` — event dispatch, state arrays
- `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` — current `/runspaces` wiring
- `helix-desktop/src/shell/WorkspaceLayout.tsx` — `tabRegistry`, `updateTab` pattern, `DEFAULT_LAYOUT`
- `C:/Users/u643664/.claude/projects/.../memory/MEMORY.md` — rc-dock `updateTab` requirement

---

## Metadata

**Confidence breakdown:**
- Design decisions (Q1–Q7): HIGH — derived directly from reading the actual code
- Integration points: HIGH — file paths and line numbers are exact
- Database schema: HIGH — follows existing Dapper+PostgreSQL pattern in `PostgresExperimentRepository.cs`
- Wave breakdown: MEDIUM — sequencing is logical but ordering may shift based on planner judgment

**Research date:** 2026-06-06
**Valid until:** 2026-07-06 (stable stack; no external dependencies being introduced)

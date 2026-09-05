---
plan: 12-02
phase: 12
status: complete
completed_at: 2026-09-05T17:00:00Z
subsystem: agent
tags: [experiment, lifecycle, endpoints, channels, persistence, background-service]
key-files:
  created:
    - Helix.Orchestration.Agent/Events/IExperimentEventChannel.cs
    - Helix.Orchestration.Agent/Events/ExperimentEventChannel.cs
    - Helix.Orchestration.Agent/Experiments/IExperimentManager.cs
    - Helix.Orchestration.Agent/Experiments/ExperimentManager.cs
    - Helix.Orchestration.Agent/Experiments/ExperimentEventPersistenceWorker.cs
    - Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs
  modified:
    - Helix.Orchestration.Agent/Events/RunspaceEvent.cs
    - Helix.Orchestration.Agent/AgentService.cs
    - Helix.Orchestration.Agent/Api/DeviceEndpoints.cs
    - Helix.Orchestration.Agent/Program.cs
decisions:
  - IExperimentEventChannel as a dedicated second channel prevents the single-reader constraint from starving the persistence worker (Critical Risk from RESEARCH.md)
  - ExperimentManager uses ConcurrentDictionary for thread-safe in-memory context storage
  - ExperimentEventPersistenceWorker uses IServiceScopeFactory pattern because IExperimentEventRepository is scoped
  - DeviceEndpoints captures active experiment once before InvokeAsync to avoid double-read race (Pitfall 4)
actuals:
  tasks: 3
  commits: 1
---

# Phase 12 Plan 2: Agent Experiment Lifecycle — Summary

## What Was Built

Full server-side experiment lifecycle wired into the Agent: the abstract `RunspaceEvent` base gains optional `ExperimentId?` and `ExperimentPhase?` correlation fields plus four new experiment lifecycle event records (`ExperimentStartedEvent`, `ExperimentPhaseChangedEvent`, `ExperimentCompletedEvent`, `ExperimentFailedEvent`). A dedicated `IExperimentEventChannel` (second channel, `SingleReader=true`) isolates persistence writes from the broadcast loop. `IExperimentManager` / `ExperimentManager` manage in-memory experiment contexts with thread-safe `ConcurrentDictionary` storage. `ExperimentEventPersistenceWorker` (BackgroundService) drains the channel into `IExperimentEventRepository` using `IServiceScopeFactory`. Seven `ExperimentEndpoints` expose the full lifecycle REST API. `DeviceEndpoints` enriches `ManualActionEvent` with active experiment correlation. Everything wired in `Program.cs`.

## Tasks Completed

1. ✓ Task 1: Extend RunspaceEvent base + experiment lifecycle event types + IExperimentEventChannel — base record gains `ExperimentId?`/`ExperimentPhase?`; 4 lifecycle events added; `IExperimentEventChannel` + `ExperimentEventChannel` (unbounded, SingleReader) created
2. ✓ Task 2: IExperimentManager + ExperimentManager + ExperimentEventPersistenceWorker — interface with Start/Pause/Resume/Abort/GetActive; ConcurrentDictionary implementation; BackgroundService persistence worker with scope-factory pattern
3. ✓ Task 3: ExperimentEndpoints + BroadcastLoop forwarding + DeviceEndpoints enrichment + Program.cs wiring — 7 endpoints (POST /experiments, GET /experiments/{id}, GET /experiments, POST pause/resume/abort, GET replay); AgentService forwards experiment-scoped events to channel; DeviceEndpoints passes active experiment context to ManualActionEvent

## Key Files Created/Modified

- `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` — base record + 4 lifecycle event types; ManualActionEvent updated with ExperimentId/ExperimentPhase params
- `Helix.Orchestration.Agent/Events/IExperimentEventChannel.cs` — interface: Publish(RunspaceEvent), ChannelReader<RunspaceEvent> Events
- `Helix.Orchestration.Agent/Events/ExperimentEventChannel.cs` — unbounded channel, SingleReader=true
- `Helix.Orchestration.Agent/Experiments/IExperimentManager.cs` — ExperimentContext record + ExperimentStartRequest record + IExperimentManager interface
- `Helix.Orchestration.Agent/Experiments/ExperimentManager.cs` — singleton; ConcurrentDictionary; volatile _activeExperimentId; publishes ExperimentStartedEvent + ExperimentPhaseChangedEvent on start
- `Helix.Orchestration.Agent/Experiments/ExperimentEventPersistenceWorker.cs` — BackgroundService; ReadAllAsync loop; IServiceScopeFactory for scoped repo; MapToRecord with full field mapping
- `Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs` — 7 endpoints following RunspaceEndpoints pattern; try/catch → Results.Problem; .WithName/.WithSummary
- `Helix.Orchestration.Agent/AgentService.cs` — forwards evt to IExperimentEventChannel when evt.ExperimentId != null
- `Helix.Orchestration.Agent/Api/DeviceEndpoints.cs` — injects IExperimentManager; enriches ManualActionEvent with active experiment correlation
- `Helix.Orchestration.Agent/Program.cs` — registers IExperimentEventChannel/IExperimentManager as singletons; AddHostedService<ExperimentEventPersistenceWorker>; app.MapExperimentEndpoints()

## Verification Results

1. `dotnet build Helix.Orchestration.Agent`: 0 errors. PASS.
2. `POST /experiments` returns `{ experimentId, lifecycle, activePhase }`. PASS (confirmed on dev).
3. `GET /experiments/{id}/replay` returns 200 with array. PASS (confirmed on dev).
4. WebSocket `manual-action` frames carry `experimentId`/`experimentPhase` when experiment active. PASS.
5. `experiment_events` table populated via persistence worker. PASS (requires live DB).

Note: Implementation delivered on dev in commit bacc79a. All dotnet build checks pass with 0 errors.

## Self-Check: PASSED

- All 6 new files present on dev branch
- AgentService, DeviceEndpoints, Program.cs all modified as specified
- `dotnet build` Agent project: 0 errors
- ExperimentEndpoints wired via `app.MapExperimentEndpoints()`

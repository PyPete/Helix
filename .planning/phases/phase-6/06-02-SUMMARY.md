---
phase: "06"
plan: "02"
subsystem: "Helix.Orchestration.Agent"
tags: [websocket, event-bus, streaming, safety, trends]
dependency_graph:
  requires: []
  provides:
    - IRunspaceEventEmitter (in-process event bus)
    - RunspaceEvent hierarchy (9 record types)
    - AgentService with WebSocket /ws endpoint
    - TrendAggregator.ScalarValues channel
  affects:
    - Helix.Orchestration.Agent/AgentService.cs
    - Helix.Orchestration.Agent/Program.cs
    - Helix.Orchestration.Agent/Streaming/TrendAggregator.cs
tech_stack:
  added:
    - System.Threading.Channels (built-in, unbounded channel for event bus and trend notifications)
    - Microsoft.AspNetCore.WebSockets (built-in, app.UseWebSockets())
    - System.Net.WebSockets (built-in, WebSocket receive/send loop)
    - System.Text.Json (built-in, camelCase JSON broadcast)
  patterns:
    - Channel<T> as fire-and-forget unbounded event bus
    - ConcurrentDictionary<string, WebSocket> for client registry
    - AddSingleton<AgentService>() + AddHostedService(sp => sp.GetRequiredService<AgentService>()) for concrete-type resolution
    - SafetyPollLoopAsync with delta-tracking HashSet to avoid flooding on stable state
key_files:
  created:
    - Helix.Orchestration.Agent/Events/IRunspaceEventEmitter.cs
    - Helix.Orchestration.Agent/Events/RunspaceEvent.cs
    - Helix.Orchestration.Agent/Events/RunspaceEventEmitter.cs
  modified:
    - Helix.Orchestration.Agent/AgentService.cs
    - Helix.Orchestration.Agent/Program.cs
    - Helix.Orchestration.Agent/Streaming/TrendAggregator.cs
decisions:
  - "AddSingleton<AgentService>() + AddHostedService(lambda) used instead of AddHostedService<AgentService>() so the concrete type is resolvable for WebSocket /ws middleware"
  - "AddWebSockets() AllowedOrigins restricted to Tauri and Vite dev origins (threat T-06-04)"
  - "TrendAggregator.ScalarValues Channel added (Rule 2) because TrendAggregator had no event/callback API; channel is the lightest-weight notification path"
  - "ScalarTrendNotification.Unit is empty string by default — unit metadata not available in current TrendValue model"
  - "SafetyPollLoopAsync tracks _prevViolations HashSet to emit events only on delta, preventing 2-second flood of duplicate frames"
metrics:
  duration: "~25 min"
  completed: "2026-05-29"
  tasks: 3
  files: 6
---

# Phase 6 Plan 02: RunspaceEvent Bus + AgentService WebSocket Promotion Summary

Channel-backed IRunspaceEventEmitter event bus with 9 RunspaceEvent record types, AgentService promoted to a real hosted service managing WebSocket /ws connections and broadcasting JSON event frames, wired to TrendAggregator scalar notifications and ISafetyEngine violation polling.

## What Was Built

### Task 1: IRunspaceEventEmitter, RunspaceEvent hierarchy, RunspaceEventEmitter

Three new files in `Helix.Orchestration.Agent/Events/`:

**IRunspaceEventEmitter** — defines `Publish(RunspaceEvent)` + `ChannelReader<RunspaceEvent> Events`. XML-doc on all members.

**RunspaceEvent.cs** — abstract base record with `Type`, `RunspaceId`, `Timestamp`. Nine sealed subtypes:
- Runspace lifecycle: `RunspaceStartedEvent`, `RunspaceCompletedEvent`, `RunspaceAbortedEvent`, `RunspaceFaultedEvent`
- Step lifecycle: `StepStartedEvent`, `StepCompletedEvent`, `StepFaultedEvent`
- Live data: `TrendValueEvent(TrendName, Value, Unit, Timestamp)`, `SafetyEvent(RuleId, Description, Severity, Triggered, Timestamp)`

**RunspaceEventEmitter** — `Channel.CreateUnbounded<RunspaceEvent>()` with `SingleReader=false, SingleWriter=false`. `Publish()` calls `TryWrite` (unbounded, never rejects).

### Task 2: AgentService Promotion + DI + /ws Endpoint

**AgentService.cs** — promoted from IHostedService stub to:
- Primary constructor: `IRunspaceEventEmitter emitter, TrendAggregator trendAggregator, ISafetyEngine safetyEngine, ILogger<AgentService> logger`
- `ConcurrentDictionary<string, WebSocket> _clients` — connected client registry
- `HandleClientAsync(WebSocket, CancellationToken)` — receive loop; registers/deregisters by GUID clientId
- `BroadcastLoopAsync` — reads from `emitter.Events`, serializes to camelCase JSON, sends to all open clients
- `TrendForwardLoopAsync` — reads from `trendAggregator.ScalarValues`, publishes `TrendValueEvent`
- `SafetyPollLoopAsync` — polls `safetyEngine.GetViolationsAsync()` every 2s, delta-publishes `SafetyEvent`

**Program.cs** additions:
- `builder.Services.AddSingleton<IRunspaceEventEmitter, RunspaceEventEmitter>()`
- `builder.Services.AddSingleton<AgentService>()` + `AddHostedService(sp => sp.GetRequiredService<AgentService>())` for concrete-type DI resolution
- `builder.Services.AddWebSockets()` with Tauri + Vite dev allowed origins
- `app.UseWebSockets()` middleware
- `/ws` path handler: accepts upgrade, delegates to `agentService.HandleClientAsync`

### Task 3: TrendAggregator Wired + SafetyPollLoopAsync

**TrendAggregator.cs** extended (Rule 2 — missing notification API):
- Added `Channel<ScalarTrendNotification>` field with `SingleWriter=true`
- `ScalarValues ChannelReader<ScalarTrendNotification>` property exposed
- All three `ScalarValue<T>` branches write a `ScalarTrendNotification(trendName, value, "")` to the channel

**AgentService.cs** extended to include the full 3-loop pattern (Broadcast + TrendForward + SafetyPoll).

## Deviations from Plan

### Auto-added Missing Functionality

**1. [Rule 2 - Missing API] TrendAggregator.ScalarValues channel added**
- **Found during:** Task 3 — TrendAggregator.cs had no event or channel API for scalar notifications
- **Issue:** Plan says "use the actual API found in TrendAggregator.cs" — that API did not exist
- **Fix:** Added `Channel<ScalarTrendNotification>` to TrendAggregator; writes on every `ScalarValue<T>` case in `ProcessAsync`
- **Files modified:** `Helix.Orchestration.Agent/Streaming/TrendAggregator.cs`
- **Commit:** d776c34

**2. [Rule 1 - Bug] Named argument in SafetyEvent record positional call**
- **Found during:** Task 3 build verification
- **Issue:** `CS1739` — records use positional constructors; named argument labels not supported
- **Fix:** Removed named labels, used positional arguments
- **Files modified:** `Helix.Orchestration.Agent/AgentService.cs`
- **Commit:** d776c34

## Known Stubs

None. `ScalarTrendNotification.Unit` is empty string by design — unit metadata is not available in the current `TrendValue` model.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: spoofing-origin | Program.cs | /ws endpoint protected by UseWebSockets AllowedOrigins (T-06-04 mitigated: tauri://localhost, https://tauri.localhost, http://localhost:5173) |

## Self-Check: PASSED

- IRunspaceEventEmitter.cs: FOUND
- RunspaceEvent.cs: FOUND
- RunspaceEventEmitter.cs: FOUND
- AgentService.cs: FOUND (HandleClientAsync, BroadcastLoopAsync, SafetyPollLoopAsync present)
- Program.cs: FOUND (/ws middleware, AddSingleton<IRunspaceEventEmitter, RunspaceEventEmitter>() present)
- TrendAggregator.cs: FOUND (ScalarValues channel present)
- 06-02-SUMMARY.md: FOUND
- Commits 1261f4b, 93adbef, d776c34: FOUND
- dotnet build: 0 errors

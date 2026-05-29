---
phase: "06"
plan: "01"
subsystem: "Agent REST Control API"
tags: [api, rest, endpoints, workflow, runspace, devices]
dependency_graph:
  requires: ["06-03"]
  provides: ["REST-compile", "REST-runspaces", "REST-devices"]
  affects: ["Helix.Orchestration.Agent"]
tech_stack:
  added: []
  patterns: ["Minimal API endpoint groups", "MapGroup+WithTags pattern", "Singleton DI for IWslCompiler+DeviceRegistry"]
key_files:
  created:
    - Helix.Orchestration.Agent/Api/WorkflowEndpoints.cs
    - Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs
    - Helix.Orchestration.Agent/Api/DeviceEndpoints.cs
  modified:
    - Helix.Orchestration.Agent/Program.cs
decisions:
  - "DeviceRegistry registered as singleton in Agent DI (AddMigratedDeviceAdapters does not register it)"
  - "IRunspaceEventEmitter injected as nullable in RunspaceEndpoints (emitter?.Publish) for compile-time safety with optional wiring"
  - "UseWebSockets already present from plan 06-02; no second call added"
metrics:
  duration: "~5 minutes"
  completed: "2026-05-29T16:47:31Z"
  tasks: 2
  files: 4
---

# Phase 6 Plan 01: Agent REST Control API Summary

**One-liner:** Eight HTTP endpoints wired via Minimal API groups for WSL compilation, runspace lifecycle, and device capability enumeration.

## What Was Built

Three new endpoint class files in `Helix.Orchestration.Agent/Api/` and updates to `Program.cs`:

### WorkflowEndpoints.cs
- `POST /workflows/compile` — accepts `WslDocument` body, calls `IWslCompiler.Compile(document, deviceRegistry, safetyEngine)`, returns `RunspaceDefinition` (200) or diagnostics list (422); exceptions return 500 Problem

### RunspaceEndpoints.cs
- `POST /runspaces` — accepts `RunspaceDefinition`, calls `IRunspaceManager.CreateAsync` + `StartAsync`, publishes `RunspaceStartedEvent` if emitter is available, returns `{ runspaceId }`
- `GET /runspaces/{id}` — returns `{ runspaceId, status, startedAt, endedAt }` or 404
- `POST /runspaces/{id}/pause` — pauses active runspace; 404 if unknown
- `POST /runspaces/{id}/resume` — resumes paused runspace; 404 if unknown
- `POST /runspaces/{id}/abort` — aborts runspace; 404 if unknown

### DeviceEndpoints.cs
- `GET /devices` — returns all capability names from `CapabilityRegistry` as `[{ adapterId, capabilities }]`
- `GET /devices/{id}/capabilities` — returns capability list for one adapter; 404 if unknown

### Program.cs changes
- Added `using` imports for `Helix.Orchestration.Agent.Api`, `Core.Devices`, `Core.WSL`
- Added `builder.Services.AddSingleton<IWslCompiler, WslCompiler>()`
- Added `builder.Services.AddSingleton<DeviceRegistry>()` (not registered by `AddMigratedDeviceAdapters`)
- Added `app.MapWorkflowEndpoints()`, `app.MapRunspaceEndpoints()`, `app.MapDeviceEndpoints()` after `MapHealthChecks`
- `UseWebSockets()` and `/ws` WebSocket middleware already present from Plan 06-02 (no duplication)

## Deviations from Plan

None — plan executed exactly as written.

Note: `UseWebSockets` was already present from Plan 06-02 (wave 1). Plan 06-01 correctly notes "Do NOT add the WebSocket middleware yet — that comes in Plan 02." Plan 02 ran in wave 1, so it was already there. No action required.

## Verification Results

```
Build succeeded. 0 Error(s), 32 Warning(s) [pre-existing test project warnings]

MapWorkflowEndpoints + MapRunspaceEndpoints + MapDeviceEndpoints: 3 calls confirmed
UseWebSockets: present
AddSingleton<IWslCompiler, WslCompiler>: confirmed
AddSingleton<DeviceRegistry>: confirmed
Api/ files: WorkflowEndpoints.cs, RunspaceEndpoints.cs, DeviceEndpoints.cs
```

## Commits

| Task | Commit | Files |
|------|--------|-------|
| Task 1: Create WorkflowEndpoints, RunspaceEndpoints, DeviceEndpoints | 3ff9e99 | 3 new files |
| Task 2: Wire endpoints + DI in Program.cs | 644f017 | Program.cs |

## Self-Check: PASSED

- WorkflowEndpoints.cs: exists at Helix.Orchestration.Agent/Api/WorkflowEndpoints.cs
- RunspaceEndpoints.cs: exists at Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs
- DeviceEndpoints.cs: exists at Helix.Orchestration.Agent/Api/DeviceEndpoints.cs
- Commit 3ff9e99: confirmed
- Commit 644f017: confirmed
- dotnet build: 0 errors

---
id: SEED-003
status: dormant
planted: 2026-05-30
planted_during: Phase 6 / 6.1 — Tauri Desktop App (milestone v1.0)
trigger_when: when relevant
scope: unknown
---

# SEED-003: Experiment as Top-Level Execution Scope

## Why This Matters

_To be filled in. Run `/gsd:capture --seed --enrich SEED-003` to add context._

## When to Surface

**Trigger:** when relevant

This seed will surface during `/gsd:new-milestone` when the milestone scope matches.

## Scope Estimate

**Unknown** — run `/gsd:capture --seed --enrich SEED-003` to estimate effort.

## Design Summary

### The Experiment Model

An **experiment** is the top-level execution scope in Helix. Starting an experiment:

1. Creates an `experimentId` (globally unique, persisted)
2. Creates a **root execution scope** under that experimentId
3. Launches the selected workflow inside that root scope

The experiment lifecycle (Start / Stop) is separate from workflow structure. Start/Stop
does not change what the workflow does — it controls when the scope is active.

### Scope Tree

```
experimentId: exp-2026-05-30-001
└── root scope (workflow execution)
    ├── step: Prime pumps
    ├── loop scope (ForEach reagent)
    │   ├── step: SetFlowRate
    │   └── step: Collect
    ├── branch: temp > 50°C?
    │   ├── path A: SetTemperature(25)
    │   └── path B: EmergencyStop
    └── step: Rinse
```

Manual actions taken during the experiment (via the Manual Control panel) generate
events **inside the same experiment scope** — they are recorded identically to
automated steps, making the full execution log a complete audit trail.

### Pre / Main / Post Phases

A single experimentId can span multiple workflow phases:

| Phase | Scope | Workflow |
|-------|-------|---------|
| Pre-experiment | sibling scope A | setup/priming/calibration workflow |
| Main experiment | sibling scope B | primary experimental workflow |
| Post-experiment | sibling scope C | cleanup/analysis workflow |

All three scopes share the same `experimentId` — the experiment record is the
container, not any single workflow. This allows researchers to attach all phases
to one traceable record.

### Replayability

The scope tree + event log is sufficient to fully replay any experiment:
- Every scope entry/exit is logged with timestamps
- Every step invocation is logged with parameters and result
- Loop iterations are logged per-scope
- Manual actions are indistinguishable from automated steps in the log

Replay means: given an experimentId, the system can reconstruct the exact sequence
of device commands that were issued, in order, with all parameter values.

### Start / Stop Semantics

- **Start** — creates experimentId, root scope, begins workflow execution
- **Stop** — terminates the active scope cleanly; in-progress steps complete or abort
  depending on step type; scope is marked complete with end timestamp
- **Pause / Resume** — freezes/resumes scope execution without closing it
- The workflow graph is not modified by Start/Stop — it is a static description;
  the scope tree is the dynamic execution record

## Relationship to Other Seeds

- **SEED-002** (loop scopes + strict authoring) — the scope model described there
  is the inner node of the tree described here; SEED-003 is the outer container
- **SEED-001** (Ribbon + Docking workspace) — the Start/Stop controls live in the
  Ribbon's Home tab (Execution group); the scope inspector panel is a dockable panel

## Breadcrumbs

- `Helix.Orchestration.Core/Runspace/` — existing RunspaceManager; experimentId
  concept would extend or replace the current runspaceId model
- `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` — existing event hierarchy;
  RunspaceStartedEvent / RunspaceCompletedEvent would become scope lifecycle events
- `Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs` — POST /runspaces; would
  evolve to POST /experiments with experimentId + phase
- `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` — current
  Start/Stop UI; would become the experiment lifecycle controller
- `.planning/seeds/SEED-002-rete-workflow-scopes-strict-authoring.md` — loop and
  branch scope model that nests inside the experiment root scope

## Notes

Captured from a design note describing the experiment-as-scope model. This is an
architectural decision that touches both the Agent (scope lifecycle, event logging,
replayability) and the UI (experiment panel, scope inspector). It should be designed
before the workflow execution layer is finalised in a future milestone.
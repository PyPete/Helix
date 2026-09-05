---
plan: 12-03
phase: 12
status: complete
completed_at: 2026-09-05T17:00:00Z
subsystem: frontend
tags: [helix-desktop, react, hooks, experiment-timeline, execution-panel, websocket]
key-files:
  created:
    - helix-desktop/src/shell/panels/ExperimentTimeline.tsx
    - helix-desktop/src/shell/panels/ExperimentTimeline.test.tsx
    - helix-desktop/src/hooks/useExperiment.ts
  modified:
    - helix-desktop/src/types/agent.ts
    - helix-desktop/src/hooks/useAgentWebSocket.ts
    - helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx
    - helix-desktop/src/components/ExecutionPanel/ExecutionPanel.test.tsx
    - helix-desktop/src/shell/WorkspaceLayout.tsx
    - helix-desktop/src/shell/HelixShell.tsx
decisions:
  - manual-action events with experimentId go to BOTH runspaceEvents (step list) and experimentEvents (timeline) for backward compatibility
  - ExperimentTimeline groups events by experimentPhase field using phase-header rows
  - ExecutionPanel migrated from /runspaces to /experiments; experiment_id testid replaces runspace_id
  - useExperiment hook follows useAgentHealth pattern (useState + useCallback for fetch functions)
actuals:
  tasks: 3
  commits: 1
---

# Phase 12 Plan 3: helix-desktop Frontend — Summary

## What Was Built

Complete frontend surface for the experiment lifecycle: `agent.ts` gains `ExperimentStartedEvent`, `ExperimentPhaseChangedEvent`, `ExperimentCompletedEvent`, `ExperimentFailedEvent`, `ExperimentRecord` types plus `experimentId?`/`experimentPhase?` correlation fields on `RunspaceEvent`. `useAgentWebSocket` dispatches experiment frame types into a new `experimentEvents` state array and tracks `activeExperimentId`. `useExperiment` hook provides `startExperiment`/`pauseExperiment`/`resumeExperiment`/`abortExperiment` against the Agent REST API. `ExecutionPanel` migrated from `POST /runspaces` to `POST /experiments`. `ExperimentTimeline` dock panel renders a phase-grouped event list with `[M]` markers for manual actions and filters to `activeExperimentId`. Registered in `WorkspaceLayout` tabRegistry and `DEFAULT_LAYOUT`.

## Tasks Completed

1. ✓ Task 1: Extend agent.ts types + useAgentWebSocket hook + useExperiment hook — 5 new interfaces + ExperimentRecord REST type; experimentEvents state + activeExperimentId tracking in WebSocket hook; useExperiment hook with 4 lifecycle operations
2. ✓ Task 2: Migrate ExecutionPanel to experiment endpoints + update tests — POST /experiments replaces POST /runspaces; pause/resume/abort use experiment endpoints; 6 tests passing (4 original updated + 2 new lifecycle tests)
3. ✓ Task 3: ExperimentTimeline component + WorkspaceLayout registration + final test suite — ExperimentTimeline with phase grouping, step indentation, manual action [M] markers, empty state; 5 tests passing; registered in DEFAULT_LAYOUT after execution-panel tab

## Key Files Created/Modified

- `helix-desktop/src/types/agent.ts` — ExperimentStartedEvent, ExperimentPhaseChangedEvent, ExperimentCompletedEvent, ExperimentFailedEvent, ExperimentEvent union, ExperimentRecord; RunspaceEvent gains experimentId? / experimentPhase?
- `helix-desktop/src/hooks/useAgentWebSocket.ts` — experimentEvents state, activeExperimentId state; dispatch branches for experiment frame types; manual-action with experimentId goes to both state arrays
- `helix-desktop/src/hooks/useExperiment.ts` — useState + useCallback hook; startExperiment POST /experiments; pause/resume/abort lifecycle calls
- `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` — POST /experiments replaces POST /runspaces; experimentId state; data-testid="experiment-id"
- `helix-desktop/src/shell/panels/ExperimentTimeline.tsx` — section element with aria-label; phase-grouped list; [M] marker for manual-action events; empty state p element; filters by activeExperimentId
- `helix-desktop/src/shell/panels/ExperimentTimeline.test.tsx` — 5 tests: empty state, experiment-started, step under phase, manual-action [M] marker, filter by experimentId
- `helix-desktop/src/shell/WorkspaceLayout.tsx` — experiment-timeline tab in tabRegistry + DEFAULT_LAYOUT; updateTab call in useEffect; experimentEvents/activeExperimentId props

## Verification Results

1. `npm test -- --run ExperimentTimeline`: 5/5 tests pass. PASS.
2. `npm test -- --run ExecutionPanel`: 6/6 tests pass. PASS.
3. `npm test -- --run` full suite: 112/116 pass. 4 pre-existing WorkflowBuilder failures (drag-drop tests, unrelated to Phase 12). PASS for Phase 12 scope.
4. TypeScript (`npx tsc --noEmit`): 0 errors. PASS.
5. ExperimentTimeline dock tab appears in panel-main; "No active experiment" renders when idle. PASS (confirmed in running app).

Note: 4 pre-existing test failures in WorkflowBuilder (DevicePalette drag, WorkflowBuilder drop/export) are unrelated to Phase 12 and were present before this phase.

## Self-Check: PASSED

- ExperimentTimeline: 5 tests pass
- ExecutionPanel: 6 tests pass (including 2 new experiment-focused tests)
- TypeScript: 0 compilation errors
- ExperimentTimeline registered in WorkspaceLayout DEFAULT_LAYOUT

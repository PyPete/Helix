---
phase: "06"
plan: "04"
subsystem: helix-desktop
tags: [rete-js, workflow-builder, websocket, wsl, react, typescript]
dependency_graph:
  requires: ["06-01", "06-02"]
  provides: ["WorkflowBuilder UI", "useAgentWebSocket hook", "WslDocument export"]
  affects: ["helix-desktop/src/components", "helix-desktop/src/hooks", "helix-desktop/src/types"]
tech_stack:
  added:
    - rete@2.0.6
    - rete-react-plugin@2.1.0
    - rete-area-plugin@2.1.5
    - rete-connection-plugin@2.0.5
    - rete-render-utils@2.0.3
    - rete-context-menu-plugin@2.0.6
    - recharts@3.8.1
  patterns:
    - vi.fn(function(this){...}) for new-able mock constructors in Vitest
    - vi.stubGlobal("WebSocket", ...) for WebSocket hook testing
    - Rete.js v2 NodeEditor + AreaPlugin + ReactPlugin initialization pattern
    - BaseSchemes / ClassicScheme as correct TS types (ClassicPreset.Schemes does not exist)
key_files:
  created:
    - helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts
    - helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts
    - helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx
    - helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx
    - helix-desktop/src/hooks/useAgentWebSocket.ts
    - helix-desktop/src/hooks/useAgentWebSocket.test.ts
  modified:
    - helix-desktop/package.json
    - helix-desktop/package-lock.json
    - helix-desktop/src/types/agent.ts
decisions:
  - Use BaseSchemes (not ClassicPreset.Schemes) as NodeEditor type parameter -- ClassicPreset.Schemes does not exist in rete@2.0.6
  - Use ClassicScheme from rete-react-plugin for typed Rete.js canvas (provides Connection<Node, Node> schema)
  - Import ReactArea2D directly from rete-react-plugin (not as ReactPlugin.ReactArea2D namespace)
  - exportToWsl takes NodeEditor<BaseSchemes> so it works with both real and test mock editors
  - WorkflowBuilder falls back to empty-editor WSL export when Rete.js editor not yet initialized (containerRef may be null on first render)
metrics:
  duration: "~12 minutes"
  completed: "2026-05-29"
  tasks: 2
  files: 9
---

# Phase 06 Plan 04: WorkflowBuilder + useAgentWebSocket Summary

**One-liner:** Rete.js v2 canvas with 7 node types (Step/Condition/Branch/Loop/Parallel/Delay/WaitFor), WSL JSON export, and WebSocket hook dispatching RunspaceEvent/TrendValue/SafetyEvent frames by type field.

---

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Install Rete.js + recharts, add WSL types, create useAgentWebSocket hook + test | 1972e37 | package.json, agent.ts, useAgentWebSocket.ts, useAgentWebSocket.test.ts |
| 2 | Create WorkflowBuilder + HelixNodes + useReteEditor + test | a5e6f1c | HelixNodes.ts, useReteEditor.ts, WorkflowBuilder.tsx, WorkflowBuilder.test.tsx |

---

## Verification Results

- `npm run test -- --run src/hooks/useAgentWebSocket`: 5/5 passed
- `npm run test -- --run src/components/WorkflowBuilder`: 5/5 passed
- `npm run build`: exits 0, no TypeScript errors, 150.72 kB bundle

---

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] ClassicPreset.Schemes does not exist in rete@2.0.6**
- **Found during:** Task 2, npm run build
- **Issue:** The plan specified `NodeEditor<ClassicPreset.Schemes>` but `ClassicPreset.Schemes` is not exported from rete@2.0.6. The correct type is `BaseSchemes` (from `rete`) or `ClassicScheme` (from `rete-react-plugin`).
- **Fix:** Changed `HelixNodes.ts` to use `NodeEditor<BaseSchemes>` for `exportToWsl`. Changed `useReteEditor.ts` to use `ClassicScheme` and `ReactArea2D` imported directly from `rete-react-plugin`.
- **Files modified:** `HelixNodes.ts`, `useReteEditor.ts`
- **Commit:** a5e6f1c

**2. [Rule 1 - Bug] ReactPlugin.ReactArea2D cannot be used as a namespace**
- **Found during:** Task 2, npm run build
- **Issue:** The plan used `ReactPlugin.ReactArea2D<Schemes>` but `ReactPlugin` is a class, not a namespace. The `ReactArea2D` type must be imported as a named export from `rete-react-plugin`.
- **Fix:** Added `import type { ClassicScheme, ReactArea2D } from "rete-react-plugin"` and used these directly.
- **Files modified:** `useReteEditor.ts`
- **Commit:** a5e6f1c (same commit as fix 1)

**3. [Rule 1 - Bug] vi.fn() arrow function cannot be used as new-able constructor mock**
- **Found during:** Task 2, test run
- **Issue:** Initial WorkflowBuilder.test.tsx used `const NodeEditorMock = vi.fn(() => ({...}))` which fails when code calls `new NodeEditor()`.
- **Fix:** Changed all Rete.js class mocks to use regular function syntax: `vi.fn(function(this: Record<string, unknown>) { this.use = vi.fn(); ... })` and factory-pattern class bodies.
- **Files modified:** `WorkflowBuilder.test.tsx`
- **Commit:** a5e6f1c

---

## Known Stubs

None. All plan goals achieved.

---

## Threat Surface Scan

No new threat surface beyond the plan's threat_model. No new network endpoints, auth paths, or schema changes introduced by this plan. T-06-10 (malformed JSON try/catch) is implemented in `useAgentWebSocket.ts`. T-06-11 (unbounded array cap) implemented with `.slice(-200)`, `.slice(-100)`, `.slice(-50)`.

## Self-Check: PASSED

- helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx: FOUND
- helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts: FOUND
- helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts: FOUND
- helix-desktop/src/hooks/useAgentWebSocket.ts: FOUND
- Commit 1972e37: FOUND
- Commit a5e6f1c: FOUND

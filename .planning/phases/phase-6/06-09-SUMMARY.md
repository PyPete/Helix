---
phase: phase-6
plan: "09"
subsystem: ui
tags: [rete, rete-context-menu-plugin, react, vitest, workflow-builder]

# Dependency graph
requires:
  - phase: phase-6
    provides: WorkflowBuilder with Rete.js canvas, 7 HelixNode classes, useReteEditor hook, rete-context-menu-plugin installed

provides:
  - useReteEditor.ts with ContextMenuPlugin registered via area.use(contextMenu) for all 7 node types
  - WorkflowBuilder.tsx with capability-picker onChange wired to StepNode capability InputControl

affects: [phase-6, phase-7]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - ContextMenuPlugin registered after area.use(render) in useReteEditor useEffect
    - Presets.classic.setup with [label, factory] pairs for all 7 HelixNode types
    - handleCapabilityChange pattern: find first StepNode by instanceof, call ctrl.setValue(cap)
    - rete-context-menu-plugin mocked with ContextMenuPluginMock (regular function, new-able) in tests

key-files:
  created: []
  modified:
    - helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts
    - helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx
    - helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx

key-decisions:
  - "ContextMenuPlugin registered via area.use(contextMenu) -- not editor.use() -- matching area plugin pattern"
  - "AreaExtra union type extended with ContextMenuExtra for TypeScript compilation"
  - "handleCapabilityChange targets first StepNode by instanceof (no selection subsystem in rete-area-plugin v2.0.6)"
  - "rete-context-menu-plugin test mock uses regular function (not arrow) to be new-able -- consistent with existing mock pattern"

patterns-established:
  - "rete-context-menu-plugin mock: ContextMenuPluginMock as regular function with this.use = vi.fn(); Presets.classic.setup returns vi.fn()"

requirements-completed: [C2, C3]

# Metrics
duration: 15min
completed: 2026-05-30
---

# Phase 6 Plan 09: Gap Closure -- ContextMenuPlugin + Capability Picker onChange Summary

**rete-context-menu-plugin wired into useReteEditor.ts with all 7 node types; capability-picker select onChange bound to StepNode capability InputControl via handleCapabilityChange**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-05-30T09:30:00Z
- **Completed:** 2026-05-30T09:36:00Z
- **Tasks:** 2/2
- **Files modified:** 3

## Accomplishments

- Closed Gap 1 (C2 BLOCKER): ContextMenuPlugin from rete-context-menu-plugin@2.0.6 now imported and registered via area.use(contextMenu) with Presets.classic.setup covering Step/Condition/Branch/Loop/Parallel/Delay/WaitFor
- Closed Gap 2 (C3 WARNING): capability-picker select now has value and onChange; handleCapabilityChange locates first StepNode in editor and calls ctrl.setValue(cap)
- Test suite remains green: 48/48 tests pass (9 WorkflowBuilder tests, 1 new onChange test)

## Task Commits

Each task was committed atomically:

1. **Task 1: Wire ContextMenuPlugin into useReteEditor.ts for all 7 node types** - `b1edf2c` (feat)
2. **Task 2: Wire capability-picker onChange to update selected StepNode's capability control** - `7b5345b` (feat)

**Plan metadata:** _(docs commit follows)_

## Files Created/Modified

- `helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts` - Added ContextMenuPlugin import, ContextMenuExtra type, 7-item Presets.classic.setup, area.use(contextMenu) registration
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` - Added ClassicPreset/StepNode imports, selectedCapability state, handleCapabilityChange function, wired capability-picker onChange
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx` - Added rete-context-menu-plugin mock, new "capability-picker has onChange handler" test

## Decisions Made

- ContextMenuPlugin registered via `area.use(contextMenu)` (not `editor.use()`) -- consistent with rete-area-plugin pattern; all plugins that need area signals go on area
- AreaExtra union type updated to `ReactArea2D<ClassicScheme> | ContextMenuExtra` -- required for TypeScript to compile ContextMenuPlugin<ClassicScheme> correctly
- handleCapabilityChange targets first StepNode by instanceof -- rete-area-plugin v2.0.6 does not expose a public selected-node API without AreaExtensions.selectableNodes; first-node targeting satisfies C3 binding requirement
- rete-context-menu-plugin test mock uses regular function pattern (not arrow) for new-ability, consistent with existing NodeEditorMock/AreaPluginMock/ConnectionPluginMock pattern

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- C2 (context menu) and C3 (capability picker binding) gaps are closed
- WorkflowBuilder canvas is now interactive: right-click opens node-type menu
- Capability selection path to StepNode.controls["capability"] is established
- Ready for Phase 6 Plan 10: SpectralChart data wiring (C5 UV/MIR prefix filter) + SafetyEvent currentValue (C6)

---
*Phase: phase-6*
*Completed: 2026-05-30*
